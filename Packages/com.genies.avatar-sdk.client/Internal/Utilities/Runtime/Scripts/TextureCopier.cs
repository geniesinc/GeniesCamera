using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Produces independent copies of <see cref="Texture"/> instances that can also be optimized with compression or
    /// transformed to <see cref="RenderTexture"/> for manipulation. It has some extra features like ensuring unique
    /// names across all copied textures or ensuring file system compatible names for GLTF exporting. It only supports
    /// <see cref="Texture2D"/> and <see cref="RenderTexture"/> as input and output types.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TextureCopier", menuName = "Genies/Material Baking/Texture Copier")]
#endif
    public sealed class TextureCopier : ScriptableObject
    {
        private static readonly Regex _whiteSpaceRegex = new(@"\s+");

        public enum CopyType
        {
            SameAsSource,
            Texture2D,
            RenderTexture,
        }

#region Inspector
        /// <summary>
        /// Texture type of the resulting copies.
        /// </summary>
        public CopyType copyType = CopyType.SameAsSource;

        /// <summary>
        /// If disabled, copy requests from the same <see cref="Texture"/> instance will always create and return a new copy.
        /// Enable this to cache created copies so copy requests to the same texture instance will only create one copy and
        /// return new references to it. Please note that this caching will assume that the given textures to copy are not
        /// modified between copy requests.
        /// </summary>
        public bool cacheCopies = true;

        /// <summary>
        /// If this is enabled the copier will check in the <see cref="ReferencedResourcesTracker"/> if the given texture to copy
        /// is referenced. If it is then the copier will return a new reference to the same texture instance instead of copying it.
        /// It is important to notice that this will ignore any other settings (output type, compression, renaming...). The only setting
        /// that overrides this behaviour is assigning any <see cref="postProcessingMaterial"/> or passing it as an argument for the
        /// copy method.
        /// </summary>
        public bool createNewRefForReferencedTextures = false;

        /// <summary>
        /// If enabled, the <see cref="TextureCopier"/> will cache all copy names and rename duplicates by adding a number
        /// suffix to ensure unique names.
        /// </summary>
        public bool cacheAndEnsureUniqueNames = false;

        /// <summary>
        /// If enabled, the <see cref="TextureCopier"/> will replace any white spaces in copy names with underscores.
        /// </summary>
        public bool noWhiteSpacesInNames = false;

        /// <summary>
        /// If enabled, the <see cref="TextureCopier"/> will remove any file system invalid characters from the copy names.
        /// </summary>
        public bool ensureFileSystemCompatibleNames = false;

        [Header("Texture2D settings")][Space(8)]
        /// <summary>
        /// Enable this so the following Texture2D settings are ignored when copying into a Texture2D, then source settings
        /// will be used instead. I.E.: if source comes with compression then copy will be compressed and viceversa.
        /// </summary>
        public bool useSameConfigAsSource = false;

        /// <summary>
        /// Whether or not to make the texture copies non-readable (only applicable to Texture2D copies).
        /// This option does not affect copying performance.
        /// </summary>
        public bool nonReadableCopies = true;

        /// <summary>
        /// If enabled, texture copies will be compressed (only applicable to Texture2D copies). Compressed
        /// textures have better rendering performance and less memory footprint but will take longer to copy.
        /// If the current platform does not support compression this will be ignored.
        /// </summary>
        public bool compressedCopies = true;

        /// <summary>
        /// If <see cref="compressedCopies"/> is enabled, enabling this will use slower algorithms for the compression
        /// process which will result in a higher quality copy. The resulting texture will have the same rendering
        /// performance and memory footprint whether you enable this option or not, the only difference will be
        /// the resulting quality and longer copying times. If you don't care about longer copying times then you should
        /// enable this option.
        /// </summary>
        public bool highQualityCompression = false;

        /// <summary>
        /// If assigned, this material will be used to perform a render pass to all copied textures. If another post-processing
        /// material is given within the copy call it will be used instead of this one. Please note that assigning a post-processing
        /// material or passing one as an argument will override the <see cref="createNewRefForReferencedTextures"/> setting behavior
        /// if it was enabled, as well as the <see cref="cacheCopies"/> setting (since we will always perform the post processing).
        /// It will also ignore <see cref="useSameConfigAsSource"/> for some specific cases, since we cannot compress to certain
        /// formats at runtime.
        /// </summary>
        public Material postProcessingMaterial;
#endregion

        // cache
        private readonly HandleCache<Texture, Texture> _copiesCache = new();
        private readonly HashSet<string> _usedNames = new();
        private readonly Dictionary<string, int> _nameDuplicatesCount = new();

        /// <summary>
        /// Clears the cached handles to copied textures and the registered names to ensuring unique names.
        /// </summary>
        public void ClearCache()
        {
            _copiesCache.Clear();
            _usedNames.Clear();
            _nameDuplicatesCount.Clear();
        }

        /// <summary>
        /// Creates a copy of the given <see cref="Texture"/> instance.
        /// </summary>
        /// <param name="name">The name for the texture copy. If not provided, the name of the source texture will be used instead</param>
        public async UniTask<Ref<Texture>> CopyAsync(Texture texture, string name = null, Material postProcessing = null)
        {
            await OperationQueue.EnqueueAsync();
            return Copy(texture, name, postProcessing);
        }

        /// <summary>
        /// Creates a copy of the given <see cref="Texture"/> instance.
        /// </summary>
        /// <param name="name">The name for the texture copy. If not provided, the name of the source texture will be used instead</param>
        public Ref<Texture> Copy(Texture texture, string name = null, Material postProcessing = null)
        {
            if (!texture)
            {
                return default;
            }

            Ref<Texture> copyRef;
            Material ppMaterial = postProcessing ? postProcessing : postProcessingMaterial;

            // having a post-processing material overrides caching or fetching new references for existing ones (we will always perform the post processing)
            if (!ppMaterial)
            {
                // if this texture instance is referenced elsewhere, create a new ref to it
                if (createNewRefForReferencedTextures && ReferencedResourcesTracker.TryGetNewReference(texture, out copyRef))
                {
                    return copyRef;
                }

                // if this texture instance was copied before and the copy is still alive, then return a new reference to it
                if (cacheCopies && _copiesCache.TryGetNewReference(texture, out copyRef))
                {
                    return copyRef;
                }
            }

            Texture textureCopy;

            try
            {
                textureCopy = CreateCopyBasedOnConfig(texture, ppMaterial);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(TextureCopier)}] something went wrong while trying to copy {texture}\n{exception}");
                return default;
            }

            if (!textureCopy)
            {
                return default;
            }

            // set the copied texture name (default to the source texture name)
            textureCopy.name = CacheAndValidateTextureName(name ?? texture.name);

            // create a reference to the copied texture and cache its handle
            copyRef = CreateRef.FromUnityObject(textureCopy);
            if (!ppMaterial && cacheCopies)
            {
                _copiesCache.CacheHandle(texture, copyRef);
            }

            // return a new reference to the handle
            return copyRef;
        }

        /// <summary>
        /// Validates the given texture name based on the current configuration and returns it.
        /// It also adds the name to the cache if <see cref="cacheAndEnsureUniqueNames"/> is enabled.
        /// </summary>
        public string CacheAndValidateTextureName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "unnamed";
            }
            else
            {
                // always trim leading and trailing white spaces
                name = name.Trim();

                // trim and replace any white spaces with underscores
                if (noWhiteSpacesInNames)
                {
                    name = _whiteSpaceRegex.Replace(name, "_");
                }

                // remove invalid characters for file names
                if (ensureFileSystemCompatibleNames)
                {
                    name = FileSystemUtils.RemoveInvalidFileNameChars(name);
                }
            }

            if (!cacheAndEnsureUniqueNames)
            {
                return name;
            }

            // if name is unique, register it and return
            if (!_usedNames.Contains(name))
            {
                _usedNames.Add(name);
                return name;
            }

            // get the current count for copies named like this
            if (!_nameDuplicatesCount.TryGetValue(name, out int count))
            {
                count = 0;
            }

            // append the count to create a unique name
            _nameDuplicatesCount[name] = ++count;
            name += $"({count})";
            _usedNames.Add(name);

            return name;
        }

        private Texture CreateCopyBasedOnConfig(Texture texture, Material postProcessing)
        {
            switch (texture)
            {
                case Texture2D texture2D:
                    bool copyTypeIsRenderTexture = copyType is CopyType.RenderTexture;
                    return CreateCopyBasedOnConfig(texture2D, copyTypeIsRenderTexture, postProcessing);

                case RenderTexture renderTexture:
                    copyTypeIsRenderTexture = copyType is not CopyType.Texture2D;
                    return CreateCopyBasedOnConfig(renderTexture, copyTypeIsRenderTexture, postProcessing);

                default:
                    throw new Exception($"Texture type {texture.GetType()} is not supported for copying");
            }
        }

        private Texture CreateCopyBasedOnConfig(Texture2D texture, bool copyTypeIsRenderTexture, Material postProcessing)
        {
            if (copyTypeIsRenderTexture)
            {
                return postProcessing ? texture.PostProcess(postProcessing) : texture.ToRenderTexture();
            }

            if (postProcessing)
            {
                RenderTexture tmpTexture = texture.PostProcessIntoTemporary(postProcessing);

                // useSameConfigAsSource is ignored here since we cannot compress to most editor imported formats at runtime, and we can't avoid creating a render texture for the post-processing
                Texture2D copy = compressedCopies ?
                    tmpTexture.ToCompressedTexture2D(highQualityCompression, nonReadableCopies) :
                    tmpTexture.ToTexture2D(nonReadableCopies);

                RenderTexture.ReleaseTemporary(tmpTexture);

                return copy;
            }

            if (useSameConfigAsSource)
            {
                return texture.CreateCopy(nonReadable: !texture.isReadable);
            }

            if (compressedCopies)
            {
                return texture.CreateCompressedCopy(highQualityCompression, nonReadableCopies);
            }

            return texture.CreateCopy(nonReadableCopies);
        }

        private Texture CreateCopyBasedOnConfig(RenderTexture renderTexture, bool copyTypeIsRenderTexture, Material postProcessing)
        {
            if (copyTypeIsRenderTexture)
            {
                RenderTexture copy = renderTexture.CreateCopy();
                if (postProcessing)
                {
                    copy.PostProcess(postProcessing);
                }

                return copy;
            }

            if (postProcessing)
            {
                RenderTexture tmpTexture = renderTexture.CreateTemporaryEmpty();
                Graphics.Blit(renderTexture, tmpTexture, postProcessing);
                Texture2D copy;

                if (useSameConfigAsSource)
                {
                    copy = tmpTexture.ToTexture2D(renderTexture.GetConfig(), nonReadable: true);
                }
                else if (compressedCopies)
                {
                    copy = tmpTexture.ToCompressedTexture2D(highQualityCompression, nonReadableCopies);
                }
                else
                {
                    copy = tmpTexture.ToTexture2D(renderTexture.GetConfig(), nonReadableCopies);
                }

                RenderTexture.ReleaseTemporary(tmpTexture);

                return copy;
            }

            if (useSameConfigAsSource)
            {
                return renderTexture.ToTexture2D(nonReadable: true); // RenderTextures are non-readable in CPU
            }

            if (compressedCopies)
            {
                return renderTexture.ToCompressedTexture2D(highQualityCompression, nonReadableCopies);
            }

            return renderTexture.ToTexture2D(nonReadableCopies);
        }
    }
}
