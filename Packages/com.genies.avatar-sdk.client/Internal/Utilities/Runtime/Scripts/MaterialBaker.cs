using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Configurable service that can bake <see cref="Material"/> instances. It will always try to use any of the
    /// <see cref="converters"/> to convert the given materials first. Materials not supported for conversion will
    /// fallback to the specified <see cref="conversionNotSupportedBehaviour"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialBaker", menuName = "Genies/Material Baking/Material Baker")]
#endif
    public sealed class MaterialBaker : ScriptableObject
    {
        /// <summary>
        /// What to do for materials that use shaders not supported for conversion.
        /// </summary>
        public enum ConversionNotSupportedBehaviour
        {
            /// <summary>
            /// Use a <see cref="MaterialCopier"/> to copy the material.
            /// </summary>
            Copy,

            /// <summary>
            /// Do nothing and return the same material instance back.
            /// </summary>
            Passthrough,

            /// <summary>
            /// Do nothing and return null.
            /// </summary>
            ReturnNull,

            /// <summary>
            /// Throw an exception.
            /// </summary>
            Throw,
        }

        public List<MaterialConverter> converters = new();
        public ConversionNotSupportedBehaviour conversionNotSupportedBehaviour;
        public MaterialCopier materialCopier;

        /// <summary>
        /// If disabled, conversions from the same <see cref="Material"/> instance will always convert and return a new instance.
        /// Enable this to cache converted materials so bake requests to the same material instance will only create one conversion and
        /// return new references to it. Please note that this caching will assume that the given materials to bake are not modified
        /// between bake requests. Materials where the conversion-not-supported behaviour is applied will follow the rules of the
        /// behaviour (i.e.: copied materials will use the caching config of the current <see cref="materialCopier"/>).
        /// </summary>
        public bool cacheConversions = true;

        // state
        private readonly Dictionary<Material, Handle<Material>> _conversionHandles = new();

        // helpers
        private readonly List<Ref<Material>> _materialRefs = new();

        /// <summary>
        /// Clears the <see cref="materialCopier"/> cache.
        /// </summary>
        public void ClearCache()
        {
            _conversionHandles.Clear();
            materialCopier?.ClearCache();
        }

        /// <summary>
        /// Same as <see cref="Bake(System.Collections.Generic.IEnumerable{UnityEngine.Material})"/> but it will
        /// return a single reference to a materials array which will release all materials when disposed. Useful if
        /// you just want to bake materials from a mesh renderer and want to assign them back without caring about
        /// individual references.
        /// </summary>
        public Ref<Material[]> SingleRefBake(IEnumerable<Material> materials)
        {
            return SingleRefBakeAsync(materials, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Bakes the given materials and returns individual references to them that will own all the
        /// generated resources (baked/copied textures). The returned array will reflect the given
        /// materials collection, meaning that if there are null or destroyed material instances
        /// the returned array will have dead references in those spots.
        /// </summary>
        public Ref<Material>[] Bake(IEnumerable<Material> materials)
        {
            return BakeAsync(materials, async: false).GetAwaiter().GetResult();
        }

        public Ref<Material> Bake(Material material)
        {
            return BakeAsync(material, async: false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Same as <see cref="Bake(System.Collections.Generic.IEnumerable{UnityEngine.Material})"/> but it will
        /// return a single reference to a materials array which will release all materials when disposed. Useful if
        /// you just want to bake materials from a mesh renderer and want to assign them back without caring about
        /// individual references.
        /// </summary>
        public UniTask<Ref<Material[]>> SingleRefBakeAsync(IEnumerable<Material> materials)
        {
            return SingleRefBakeAsync(materials, async: true);
        }

        /// <summary>
        /// Bakes the given materials and returns individual references to them that will own all the
        /// generated resources (baked/copied textures). The returned array will reflect the given
        /// materials collection, meaning that if there are null or destroyed material instances
        /// the returned array will have dead references in those spots.
        /// </summary>
        public UniTask<Ref<Material>[]> BakeAsync(IEnumerable<Material> materials)
        {
            return BakeAsync(materials, async: true);
        }

        public UniTask<Ref<Material>> BakeAsync(Material material)
        {
            return BakeAsync(material, async: true);
        }

        private async UniTask<Ref<Material>[]> BakeAsync(IEnumerable<Material> materials, bool async)
        {
            await BakeIntoMaterialRefsAsync(materials, async);
            Ref<Material>[] results = _materialRefs.ToArray();
            _materialRefs.Clear();

            return results;
        }

        private async UniTask<Ref<Material[]>> SingleRefBakeAsync(IEnumerable<Material> materials, bool async)
        {
            await BakeIntoMaterialRefsAsync(materials, async);

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

        private async UniTask<Ref<Material>> BakeAsync(Material material, bool async)
        {
            if (!material)
            {
                return default;
            }

            // if this material instance was converted before and the converted material is still alive, then return a new reference to it
            if (cacheConversions && _conversionHandles.TryGetValue(material, out Handle<Material> conversionHandle) && conversionHandle.IsAlive)
            {
                return CreateRef.FromHandle(conversionHandle);
            }

            // find a converter that supports the material and convert it
            foreach (MaterialConverter converter in converters)
            {
                if (!converter || !converter.CanConvert(material))
                {
                    continue;
                }

                Ref<Material> conversionRef;

                if (async)
                {
                    conversionRef = await converter.ConvertWithoutCheckAsync(material, materialCopier?.textureCopier);
                }
                else
                {
                    conversionRef = converter.ConvertWithoutCheck(material, materialCopier?.textureCopier);
                }

                if (!conversionRef.IsAlive)
                {
                    Debug.LogError($"[{nameof(MaterialBaker)}] conversion failed. Material: {material} | Converter: {converter}");
                    return default;
                }

                if (cacheConversions)
                {
                    _conversionHandles[material] = conversionRef.Handle;
                }

                return conversionRef;
            }

            // we found no converters that support the material so follow the configured behaviour
            return conversionNotSupportedBehaviour switch
            {
                ConversionNotSupportedBehaviour.Copy
                    => await CopyMaterialAsync(material, async),

                ConversionNotSupportedBehaviour.Passthrough
                    => CreateRef.FromAny(material), // dummy ref that will not destroy the material

                ConversionNotSupportedBehaviour.ReturnNull
                    => default, // dead reference

                ConversionNotSupportedBehaviour.Throw or _
                    => throw new Exception($"[{nameof(MaterialBaker)}] could not find a {nameof(MaterialConverter)} for shader: {material.shader.name}"),
            };
        }

        private async UniTask<Ref<Material>> CopyMaterialAsync(Material material, bool async)
        {
            if (!materialCopier)
            {
                Debug.LogError($"[{nameof(MaterialBaker)}] conversion-not-supported behaviour is set to copy but no {nameof(MaterialCopier)} has been provided");
                return default;
            }

            if (async)
            {
                return await materialCopier.CopyAsync(material);
            }
            else
            {
                return materialCopier.Copy(material);
            }
        }

        // bakes the given materials collection into the private _materialRefs list
        private async UniTask BakeIntoMaterialRefsAsync(IEnumerable<Material> materials, bool async)
        {
            _materialRefs.Clear();

            if (materials is null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                Ref<Material> bake = await BakeAsync(material, async);
                _materialRefs.Add(bake);
            }
        }
    }
}
