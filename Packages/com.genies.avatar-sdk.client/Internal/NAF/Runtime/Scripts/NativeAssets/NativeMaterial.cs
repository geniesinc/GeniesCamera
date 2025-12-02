using System;
using System.Collections.Generic;
using Genies.Refs;
using GnWrappers;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;

using Material = UnityEngine.Material;
using Object = UnityEngine.Object;
using Texture = UnityEngine.Texture;

namespace Genies.Naf
{
    /**
     * Loads and manages a native material in the Unity scene.
     * <br/><br/>
     * It contains utility method to reset the material to defaults, as well as setting any properties contained in any
     * native material. It also handles native textures so they are properly released when no longer used. If you want
     * to edit the Unity material by setting textures manually, you should use the custom NativeMaterial.SetTexture
     * methods to ensure that loaded native textures are released properly.
     */
    public sealed class NativeMaterial : IDisposable
    {
        private static  readonly Dictionary<uint, string[]> KeywordsByExtrasItemHash = new();

        public Material Material { get; private set; }

        public readonly NafMaterialSettings Settings;

        private readonly MaterialTexturePropertiesExtensions.TextureRefPropertyProcessor _textureProcessor;
        private readonly Dictionary<int, Ref<Texture>> _textureRefsByNameId = new();
        private readonly List<Ref<Texture>> _texturesCache = new();

        private Material _defaultMaterial;

        public NativeMaterial(NafMaterialSettings settings = null)
        {
            Settings = settings ? settings : NafMaterialSettings.Default;
            _textureProcessor = SetTexture;
        }

        public NativeMaterial(NativeMaterial other) : this()
        {
            if (!other.Material)
            {
                return;
            }

            Material = new Material(other.Material);
            Settings = other.Settings;

            _defaultMaterial = new Material(other._defaultMaterial);

            foreach ((int nameId, Ref<Texture> textureRef) in other._textureRefsByNameId)
            {
                _textureRefsByNameId.Add(nameId, textureRef.New());
            }
        }

        public NativeMaterial(GnWrappers.Material wrapper) : this()
        {
            SetMaterial(wrapper);
        }

        /**
         * Sets the current Unity material to match the given native material (shader, name, properties and keywords).
         */
        public void SetMaterial(GnWrappers.Material nativeMaterial)
        {
            if (nativeMaterial is null)
            {
                ClearMaterial();
                return;
            }

            // try to set the shader (this creates the material if not created yet)
            if (!SetShader(nativeMaterial))
            {
                ClearMaterial();
                return;
            }

            // set name, properties and keywords from the native material
            Material.name = nativeMaterial.Name();
            SetProperties(nativeMaterial);
            SetKeywords(nativeMaterial);
        }

        /**
         * Tries to set the material shader to the one defined by the given native material model.
         */
        public bool SetShader(GnWrappers.Material nativeMaterial)
        {
            return SetShader(nativeMaterial.Model());
        }

        /**
         * Sets the material shader by NAF model. This creates a material if currently null.
         */
        public bool SetShader(string model)
        {
            Material defaultMaterial = Settings.CreateMaterial(model);
            if (defaultMaterial)
            {
                return SetShader(defaultMaterial, ownMaterial: true);
            }

            return false;
        }

        /**
         * Sets the material shader. This creates a material if currently null.
         */
        public bool SetShader(Shader shader)
        {
            Material defaultMaterial = Settings.CreateMaterial(shader);
            if (defaultMaterial)
            {
                return SetShader(defaultMaterial, ownMaterial: true);
            }

            return false;
        }

        /**
         * Sets a new default material and updates the Material shader. This creates a new material if currently null.
         */
        public bool SetShader(Material defaultMaterial, bool ownMaterial = true)
        {
            if (!defaultMaterial)
            {
                Debug.LogError("Failed to set the native material shader: default material is null or destroyed");
                return false;
            }

            // set the new default material
            if (_defaultMaterial)
            {
                Object.Destroy(_defaultMaterial);
            }
            _defaultMaterial = ownMaterial ? defaultMaterial : new Material(defaultMaterial);

            // if the Material was not set already, create a new one and return
            if (!Material)
            {
                Material = new Material(_defaultMaterial);
                return true;
            }

            // update the Material's shader if not the same
            if (Material.shader != _defaultMaterial.shader)
            {
                Material.shader = _defaultMaterial.shader;
            }

            return true;
        }

        /**
         * Resets properties to their defaults and adds the properties contained on the native material. This method
         * also handles special caching for texture properties so native texture references that are unset will not be
         * released until the end (avoiding recreating Unity textures).
         */
        public void SetProperties(GnWrappers.Material nativeMaterial)
        {
            CacheCurrentNativeTextures();

            try
            {
                ResetPropertiesToDefault();
                AddProperties(nativeMaterial);
            }
            finally
            {
                ReleaseCachedNativeTextures();
            }
        }

        /**
         * Adds all the properties contained on the native material to the current Unity material. Shader is ignored
         * and any previously set properties that are not overriden by the native material will remain.
         */
        public void AddProperties(GnWrappers.Material nativeMaterial)
        {
            // set all non-texture properties that we support from native (Vector4 are the same as Colors in Unity)
            AddFloatProperties(nativeMaterial);
            AddIntProperties(nativeMaterial);
            AddVector4Properties(nativeMaterial);
            AddMatrix4Properties(nativeMaterial);
            AddTextureProperties(nativeMaterial);
        }

        public void AddFloatProperties(GnWrappers.Material nativeMaterial)
        {
            using MaterialProperties floatProperties = nativeMaterial.Floats();
            floatProperties.Process<float>(Material.SetFloat);
        }

        public void AddIntProperties(GnWrappers.Material nativeMaterial)
        {
            using MaterialProperties intProperties = nativeMaterial.Ints();
            intProperties.Process<int>(Material.SetInteger);
        }

        public void AddVector4Properties(GnWrappers.Material nativeMaterial)
        {
            using MaterialProperties vectorProperties = nativeMaterial.Vector4s();
            vectorProperties.Process<Vector4>(Material.SetVector);
        }

        public void AddMatrix4Properties(GnWrappers.Material nativeMaterial)
        {
            using MaterialProperties matrixProperties = nativeMaterial.Matrix4s();
            matrixProperties.Process<Matrix4x4>(Material.SetMatrix);
        }

        public void AddTextureProperties(GnWrappers.Material nativeMaterial)
        {
            // set texture properties using our custom SetTexture method as a processor (so we save the references internally)
            using MaterialTextureProperties textureProperties = nativeMaterial.Texture2ds();
            textureProperties.ProcessAsUnityTextures(_textureProcessor);
        }

        /**
         * Sets the given texture to the Unity material. You must use this method to ensure that any texture references
         * are released.
         */
        public void SetTexture(string name, Texture value)
        {
            SetTexture(Shader.PropertyToID(name), value);
        }

        /**
         * Sets the given texture to the Unity material. You must use this method to ensure that any texture references
         * are released.
         */
        public void SetTexture(int nameId, Texture value)
        {
            if (_textureRefsByNameId.Remove(nameId, out Ref<Texture> textureRef))
            {
                textureRef.Dispose();
            }

            Material.SetTexture(nameId, value);
        }

        /**
         * Sets and saves the given texture reference to the Unity material.
         */
        public void SetTexture(string name, Ref<Texture> value)
        {
            SetTexture(Shader.PropertyToID(name), value);
        }

        /**
         * Sets and saves the given texture reference to the Unity material.
         */
        public void SetTexture(int nameId, Ref<Texture> value)
        {
            if (_textureRefsByNameId.Remove(nameId, out Ref<Texture> textureRef))
            {
                textureRef.Dispose();
            }

            if (value.IsAlive)
            {
                _textureRefsByNameId.Add(nameId, value);
            }

            Material.SetTexture(nameId, value.Item);
        }

        /**
         * Resets the material to its default properties.
         */
        public void ResetPropertiesToDefault()
        {
            if (!Material)
            {
                return;
            }

            // reset properties to the defaults
            Material.CopyPropertiesFromMaterial(_defaultMaterial);

            // release textures
            foreach (Ref textureRef in _textureRefsByNameId.Values)
            {
                textureRef.Dispose();
            }

            _textureRefsByNameId.Clear();
        }

        /**
         * Resets the material keywords to the defaults.
         */
        public void ResetKeywordsToDefault()
        {
            if (!Material)
            {
                return;
            }

            LocalKeyword[] currentKeywords = Material.enabledKeywords;
            LocalKeyword[] defaultKeywords = _defaultMaterial.enabledKeywords;

            foreach (LocalKeyword currentKeyword in currentKeywords)
            {
                Material.DisableKeyword(currentKeyword);
            }

            foreach (LocalKeyword defaultKeyword in defaultKeywords)
            {
                Material.EnableKeyword(defaultKeyword);
            }
        }

        /**
         * Sets the keywords on the material to match the ones on the given native material.
         */
        public void SetKeywords(GnWrappers.Material nativeMaterial)
        {
            ResetKeywordsToDefault();
            EnableKeywords(nativeMaterial);
        }

        /**
         * Enable shader keywords on the native material (will not disable already enabled ones that are not on the given material).
         */
        public void EnableKeywords(GnWrappers.Material nativeMaterial)
        {
            // check for an extras entity
            using Entity extrasEntity = nativeMaterial.Extras();
            if (extrasEntity.IsNull())
            {
                return;
            }

            // check for an EntityExtras attribute on the extras entity
            using EntityExtras extras = EntityExtras.GetFrom(extrasEntity);
            if (extras.IsNull())
            {
                return;
            }

            // iterate over the extras, if any of them contains shader keywords, enable them
            uint extrasSize = extras.Size();
            for (uint i = 0; i < extrasSize; ++i)
            {
                using EntityExtrasItem item = extras.Get(i);
                EnableKeywords(item);
            }
        }

        /**
         * If the given extras item is a shader keywords item, it will enable the keywords it contains on the material.
         */
        public void EnableKeywords(EntityExtrasItem item)
        {
            IReadOnlyList<string> keywords = TryGetShaderKeywords(item);
            if (keywords is null)
            {
                return;
            }

            foreach (string keyword in keywords)
            {
                Material.EnableKeyword(keyword);
            }
        }

        public void ClearMaterial()
        {
            if (Material)
            {
                Object.Destroy(Material);
            }

            if (_defaultMaterial)
            {
                Object.Destroy(_defaultMaterial);
            }

            Material = null;
            _defaultMaterial = null;

            foreach (Ref textureRef in _textureRefsByNameId.Values)
            {
                textureRef.Dispose();
            }

            _textureRefsByNameId.Clear();
        }

        /**
         * Clears all the native textures currently set to the unity material and release their references.
         */
        public void ClearNativeTextures()
        {
            foreach ((int nameId, Ref<Texture> textureRef) in _textureRefsByNameId)
            {
                Material.SetTexture(nameId, null);
                textureRef.Dispose();
            }

            _textureRefsByNameId.Clear();
        }

        /**
         * Caches references to all currently set native textures. You can release them later by calling ReleaseCachedNativeTextures.
         */
        public void CacheCurrentNativeTextures()
        {
            ReleaseCachedNativeTextures();
            foreach (Ref<Texture> textureRef in _textureRefsByNameId.Values)
            {
                if (textureRef.IsAlive)
                {
                    _texturesCache.Add(textureRef.New());
                }
            }
        }

        /**
         * Releases all cached native textures that were previously set with CacheCurrentNativeTextures.
         */
        public void ReleaseCachedNativeTextures()
        {
            foreach (Ref<Texture> textureRef in _texturesCache)
            {
                textureRef.Dispose();
            }

            _texturesCache.Clear();
        }

        public void Dispose()
        {
            ClearMaterial();
        }

        /**
         * Tries to find the Shader for the given model (the model string that comes with native material wrappers).
         */
        public static Shader FindShader(string model)
        {
            // validate that the model is a unity shader
            if (string.IsNullOrEmpty(model) || !model.StartsWith("unity/"))
            {
                return null;
            }

            // remove the "unity/" prefix and try to find the shader from the model
            return Shader.Find(model[6..]);
        }

        /**
         * If the given entity extras item contains Unity material shader keywords, it will return them. Otherwise, it returns null.
         */
        public static IReadOnlyList<string> TryGetShaderKeywords(EntityExtrasItem item)
        {
            uint dataHash = item.DataHash();
            if (KeywordsByExtrasItemHash.TryGetValue(dataHash, out string[] keywords))
            {
                return keywords;
            }

            if (item.Type() != "shaderKeywords/json")
            {
                return null;
            }

            using DynamicAccessor dataAccessor = item.DataAccessor();
            string json = dataAccessor.GetDataAsUTF8String();
            JArray array = JArray.Parse(json);

            keywords = new string[array.Count];
            KeywordsByExtrasItemHash.Add(dataHash, keywords);

            for (int i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = array[i].Value<string>();
            };

            return keywords;
        }
    }
}
