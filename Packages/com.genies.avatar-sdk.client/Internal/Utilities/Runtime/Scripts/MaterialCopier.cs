using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Utilities
{
    /// <summary>
    /// Produces independent copies of <see cref="Material"/> instances that can also come
    /// with independent copies of the assigned <see cref="Texture2D"/> and <see cref="RenderTexture"/>
    /// properties so the material copy is completely independent from the source.
    /// <br/><br/>
    /// Textures are copied using the <see cref="textureCopier"/> which can be configured for
    /// compression and other handy optimization and utility features. All copied texture references
    /// are embedded within the returned material copy reference, which means that when this
    /// reference is disposed, all the copied textures will be disposed too.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialCopier", menuName = "Genies/Material Baking/Material Copier")]
#endif
    public sealed class MaterialCopier : ScriptableObject
    {
#region Inspector
        public TextureCopier textureCopier;

        /// <summary>
        /// If disabled, copy requests from the same <see cref="Material"/> instance will always create and return a new copy.
        /// Enable this to cache created copies so copy requests to the same material instance will only create one copy and
        /// return new references to it. Please note that this caching will assume that the given materials to copy are not
        /// modified between copy requests.
        /// </summary>
        public bool cacheCopies = true;

        /// <summary>
        /// Enable this so all the assigned textures on the source material are also copied. You can see this option as deep
        /// copying the material. Only <see cref="UnityEngine.Texture2D"/> and <see cref="UnityEngine.RenderTexture"/> textures
        /// will be copied.
        /// </summary>
        public bool copyTextures = true;

        /// <summary>
        /// If this is enabled the copier will check in the <see cref="ReferencedResourcesTracker"/> if the given material to copy
        /// is referenced. If it is then the copier will return a new reference to the same material instance instead of copying it.
        /// Be aware that the fact that a reference exist to a material instance it doesn't mean that all its current textures are
        /// referenced or included within the material reference. Use this if you know how the copied materials are being created.
        /// </summary>
        public bool createNewRefForReferencedMaterials = false;

        /// <summary>
        /// Enable this to use the <see cref="customTextureNamesFormat"/> for generating custom names for the copied textures. If
        /// disabled, the source texture name will be used instead.
        /// </summary>
        public bool useCustomNameForTextureCopies = false;

        /// <summary>
        /// The formatting string used for texture copy names if <see cref="useCustomNameForTextureCopies"/> is enabled.
        /// Available format variables are:
        /// <list type="bullet">
        /// <item>"{0}": the source material name.</item>
        /// <item>"{1}": the source texture name.</item>
        /// <item>"{2}": the shader property name that the texture is assigned to.</item>
        /// </list>
        /// Example: given a source material named "URPLitMaterial" and a source texture named "albedoMap", setting this
        /// field to <code>{0}_{1}</code> would result in the copy texture named "URPLitMaterial_albedoMap". Please note that
        /// based on the <see cref="textureCopier"/> configuration, some validation and modification of this name may be applied.
        /// </summary>
        public string customTextureNamesFormat = "{0}--{2}";

        /// <summary>
        /// Enable this so we try to instantiate material copies by using Shader.Find() on the source shader name instead of directly
        /// copying the source material. If you are using Addressables it is likely that your materials will come with independent shader
        /// instances that will be unloaded when the material asset is released, which could cause your material copy to break if its
        /// using the same shader instance.
        /// </summary>
        public bool tryToFindShaderFix = true;

        [Tooltip("If assigned it will be used to post-process every copied texture, overriding any post-processing materials set in the used texture copier")]
        public Material texturePostProcessingMaterial;

        [Tooltip("Use this to assign post-postprocessing only to specific texture properties from the copied material (it has higher precedence than the texture post processing material)")]
        public List<PostProcessedTextureProperty> postProcessedTextureProperties;
#endregion

        // state
        private readonly HandleCache<Material, Material> _copiesCache = new();

        // helpers
        private readonly List<Ref<Material>> _materialRefs = new();
        private readonly List<Ref<Texture>> _textureRefs = new();

        /// <summary>
        /// Clears the <see cref="textureCopier"/> cache and the cached handles to copied materials.
        /// </summary>
        public void ClearCache()
        {
            _copiesCache.Clear();
            textureCopier.ClearCache();
        }

        /// <summary>
        /// Same as <see cref="Copy(System.Collections.Generic.IEnumerable{UnityEngine.Material})"/> but it will
        /// return a single reference to a materials array which will release all copies when disposed. Useful if
        /// you just want to copy materials from a mesh renderer and want to assign them back without caring about
        /// individual references.
        /// </summary>
        public Ref<Material[]> SingleRefCopy(IEnumerable<Material> materials)
        {
            return SingleRefCopyAsync(materials, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Copies the given materials and returns individual references to the copies that will
        /// own all the generated resources (textures copies). The returned array will reflect the given
        /// materials collection, meaning that if there are null or destroyed material instances
        /// the returned array will have dead references in those spots.
        /// </summary>
        public Ref<Material>[] Copy(IEnumerable<Material> materials)
        {
            return CopyAsync(materials, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates a copy of the given <see cref="Material"/> instance.
        /// Returns a reference to the copied material that will destroy it when disposed, including
        /// any copied textures if <see cref="copyTextures"/> is enabled.
        /// </summary>
        public Ref<Material> Copy(Material material)
        {
            return CopyAsync(material, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Same as <see cref="Copy(System.Collections.Generic.IEnumerable{UnityEngine.Material})"/> but it will
        /// return a single reference to a materials array which will release all copies when disposed. Useful if
        /// you just want to copy materials from a mesh renderer and want to assign them back without caring about
        /// individual references.
        /// </summary>
        public UniTask<Ref<Material[]>> SingleRefCopyAsync(IEnumerable<Material> materials)
        {
            return SingleRefCopyAsync(materials, async: true);
        }

        /// <summary>
        /// Copies the given materials and returns individual references to the copies that will
        /// own all the generated resources (textures copies). The returned array will reflect the given
        /// materials collection, meaning that if there are null or destroyed material instances
        /// the returned array will have dead references in those spots.
        /// </summary>
        public UniTask<Ref<Material>[]> CopyAsync(IEnumerable<Material> materials)
        {
            return CopyAsync(materials, async: true);
        }

        /// <summary>
        /// Creates a copy of the given <see cref="Material"/> instance.
        /// Returns a reference to the copied material that will destroy it when disposed, including
        /// any copied textures if <see cref="copyTextures"/> is enabled.
        /// </summary>
        public UniTask<Ref<Material>> CopyAsync(Material material)
        {
            return CopyAsync(material, async: true);
        }

        private async UniTask<Ref<Material[]>> SingleRefCopyAsync(IEnumerable<Material> materials, bool async)
        {
            await CopyIntoMaterialRefsAsync(materials, async);

            // create the materials array and populate it
            var materialsArray = new Material[_materialRefs.Count];
            for (int i = 0; i < materialsArray.Length; ++i)
            {
                materialsArray[i] = _materialRefs[i].Item;
            }

            // create a reference to the materials array that owns all the individual references
            Ref<Material[]> packedMaterialsRef = CreateRef.FromDependentResource(materialsArray, _materialRefs);
            _materialRefs.Clear();

            return packedMaterialsRef;
        }

        private async UniTask<Ref<Material>[]> CopyAsync(IEnumerable<Material> materials, bool async)
        {
            await CopyIntoMaterialRefsAsync(materials, async);
            Ref<Material>[] results = _materialRefs.ToArray();
            _materialRefs.Clear();

            return results;
        }

        private async UniTask<Ref<Material>> CopyAsync(Material material, bool async)
        {
            if (!material)
            {
                return default;
            }

            // if this material instance is referenced elsewhere, create a new ref to it
            if (createNewRefForReferencedMaterials && ReferencedResourcesTracker.TryGetNewReference(material, out Ref<Material> materialRef))
            {
                return materialRef;
            }

            // if this material instance was copied before and the copy is still alive, then return a new reference to it
            if (cacheCopies && _copiesCache.TryGetNewReference(material, out materialRef))
            {
                return materialRef;
            }

            // create a copy of the original material
            Material materialCopy = CreateCopy(material);
            materialCopy.name = material.name;

            if (copyTextures)
            {
                materialRef = await CopyTexturesAsync(materialCopy, async);
            }
            else
            {
                materialRef = CreateRef.FromUnityObject(materialCopy);
            }

            // cache the handle for the copied material
            if (cacheCopies)
            {
                _copiesCache.CacheHandle(material, materialRef);
            }

            return materialRef;
        }

        // copies all textures from the given material and returns a reference that encapsulates all the copied texture references
        private async UniTask<Ref<Material>> CopyTexturesAsync(Material material, bool async)
        {
            (string name, int id)[] textureProperties = material.shader.GetPropertiesForType(ShaderPropertyType.Texture);
            _textureRefs.Clear();

            // copy each Texture2D or RenderTexture assigned to the material
            foreach ((string propertyName, int propertyId) in textureProperties)
            {
                Texture texture = material.GetTexture(propertyId);
                if (!texture)
                {
                    continue;
                }

                // use the TextureCopier to copy the texture
                Material ppMaterial = GetPostProcessingMaterial(propertyName);
                string textureCopyName = GetTextureCopyNameBasedOnConfig(material.name, texture.name, propertyName);
                Ref<Texture> textureCopyRef;

                if (async)
                {
                    textureCopyRef = await textureCopier.CopyAsync(texture, textureCopyName, ppMaterial);
                }
                else
                {
                    textureCopyRef = textureCopier.Copy(texture, textureCopyName, ppMaterial);
                }

                if (!textureCopyRef.IsAlive)
                {
                    continue;
                }

                // set the copied texture to the material copy
                material.SetTexture(propertyId, textureCopyRef.Item);
                _textureRefs.Add(textureCopyRef);
            }

            // create the material handle with all the copied texture references embedded
            Ref<Material> materialRef = CreateRef.FromUnityObject(material);
            materialRef = CreateRef.FromDependentResource(materialRef, _textureRefs);
            _textureRefs.Clear(); // make sure we don't hold the textures references which are owned by the handle now

            return materialRef;
        }

        // copies the given materials collection into the private _materialRefs list
        private async UniTask CopyIntoMaterialRefsAsync(IEnumerable<Material> materials, bool async)
        {
            _materialRefs.Clear();

            if (materials is null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                Ref<Material> copy = await CopyAsync(material, async);
                _materialRefs.Add(copy);
            }
        }

        private Material CreateCopy(Material source)
        {
            if (!tryToFindShaderFix)
            {
                return new Material(source);
            }

            /**
             * It's important that we copy the material this way since some materials come from Addressables.
             * The source shader instance will get destroyed when the source material is released. If we just
             * do "return new Material(source)" it will only work while the source is loaded.
             *
             * Shader.Find() will get a new shader instance for shaders loaded from Addressables and the same
             * instance for shaders loaded in a more conventional way. The only caveat is that we need to make
             * sure that all our shaders are included within the project build.
             */
            var shader = Shader.Find(source.shader.name);

            // if shader is not included in the build, then use the source's shader instance and log a warning
            if (!shader)
            {
                Debug.LogWarning($"[{nameof(MaterialCopier)}] couldn't find shader by name: \"{source.shader.name}\". This is likely happening because the shader was loaded by Addressables and it is not included within the build. The copied material will use the source's shader instance, which will be destroyed if the source material is released");
                return new Material(source);
            }

            var copy = new Material(shader);
            copy.CopyPropertiesFromMaterial(source);
            return copy;
        }

        private string GetTextureCopyNameBasedOnConfig(string sourceMaterialName, string sourceTextureName, string propertyName)
        {
            if (!useCustomNameForTextureCopies || string.IsNullOrEmpty(customTextureNamesFormat))
            {
                return null; // let the TextureCopier use the default
            }

            return string.Format(customTextureNamesFormat, sourceMaterialName, sourceTextureName, propertyName);
        }

        private Material GetPostProcessingMaterial(string propertyName)
        {
            if (postProcessedTextureProperties is null)
            {
                return texturePostProcessingMaterial;
            }

            for (int i = 0; i < postProcessedTextureProperties.Count; ++i)
            {
                if (postProcessedTextureProperties[i].propertyName == propertyName)
                {
                    return postProcessedTextureProperties[i].postProcessingMaterial;
                }
            }

            return texturePostProcessingMaterial;
        }
    }
}
