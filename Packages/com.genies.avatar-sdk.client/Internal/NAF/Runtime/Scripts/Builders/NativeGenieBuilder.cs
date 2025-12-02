using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using GnWrappers;
using UnityEngine;

namespace Genies.Naf
{
    [Serializable]
    public struct AssetLoadRequest
    {
        public string assetId;
        public Dictionary<string, string> parameters;

        public AssetLoadRequest(string assetId, Dictionary<string, string> parameters = null)
        {
            this.assetId = assetId;
            this.parameters = parameters;
        }
    }

    [RequireComponent(typeof(NativeGenie))]
    public sealed class NativeGenieBuilder : MonoBehaviour, IDisposable
    {
        public NativeGenie NativeGenie => _nativeGenie;

        /**
         * Low-level classes. Don't access them unless you know what you are doing.
         */
        public AssetBuilder         AssetBuilder { get; private set; }
        public ColorEditor          ColorEditor  { get; private set; }
        public ShapeEditor          ShapeEditor  { get; private set; }
        public MegaSkinTattooEditor TattooEditor { get; private set; }

        public bool RefittingDebuggerEnabled { get; private set; }

        public bool IgnoreMeshGroups { get => ignoreMeshGroups; set => AssetBuilder.SetIgnoreMeshGroups(ignoreMeshGroups = value); }
        public bool SkipMeshGrouping { get => skipMeshGrouping; set => AssetBuilder.SetSkipMeshGrouping(skipMeshGrouping = value); }
        public bool DisableRefitting { get => disableRefitting; set => AssetBuilder.SetDefaultRmcSettings(!(disableRefitting = value)); }

        // inspector
        public NafAssetResolverConfig assetResolverConfig;
        [Tooltip("If enabled, all meshes will be combined into the Default output mesh")]
        [SerializeField] private bool ignoreMeshGroups;
        [Tooltip("If enabled, no meshes will be combined and each one will have its own output mesh. 'ignoreMeshGroups' will do nothing while this is enabled.")]
        [SerializeField] private bool skipMeshGrouping;
        [SerializeField] private bool disableRefitting;
        [Space(16), Header("Refitting Debugger")]
        public bool refDebSkipMeshGrouping = false;
        public bool refDebSingleGroup = true;
        public bool refDebIncludeNonDeformedMeshes = false;
        public bool refDebEnableVertexAttributes = false;


        private NativeGenie  _nativeGenie;
        private ContainerApi _containerApi;

        private bool _awaked = false;

        /**
         * Use this if you want to ensure the component is awake and initialized. If Awake was already invoked, nothing
         * will happen. This is useful when instantiating the component within an inactive GameObject.
         */
        public void EnsureAwake()
        {
            Awake();
        }

        private void Awake()
        {
            if (_awaked)
            {
                return;
            }

            _awaked = true;

            _nativeGenie = GetComponent<NativeGenie>();
            _nativeGenie.EnsureAwake();

            string resolverJson = assetResolverConfig ? assetResolverConfig.Serialize() : NafAssetResolverConfig.Default.Serialize();
            _containerApi = new ContainerApi(resolverJson);
            AssetBuilder = new AssetBuilder();
            AssetBuilder.SetIgnoreMeshGroups(ignoreMeshGroups);
            AssetBuilder.SetSkipMeshGrouping(skipMeshGrouping);
            AssetBuilder.SetDefaultRmcSettings(!disableRefitting);
            RefittingDebuggerEnabled = false;

            // initialize editors
            ColorEditor = ColorEditor.GetFrom(AssetBuilder);
            ShapeEditor = ShapeEditor.GetFrom(AssetBuilder);
            TattooEditor = MegaSkinTattooEditor.GetFrom(AssetBuilder);

            if (ColorEditor.IsNull())
            {
                ColorEditor = ColorEditor.Create(AssetBuilder);
            }

            if (ShapeEditor.IsNull())
            {
                ShapeEditor = ShapeEditor.Create(AssetBuilder);
            }

            if (TattooEditor.IsNull())
            {
                TattooEditor = MegaSkinTattooEditor.Create(AssetBuilder);
            }
        }

        private void OnValidate()
        {
            if (_awaked && AssetBuilder is not null)
            {
                if (RefittingDebuggerEnabled)
                {
                    AssetBuilder.SetRmcDebugSettings(refDebSkipMeshGrouping, refDebSingleGroup, refDebIncludeNonDeformedMeshes, refDebEnableVertexAttributes);
                }
                else
                {
                    AssetBuilder.SetDefaultRmcSettings(!disableRefitting);
                }

                AssetBuilder.SetIgnoreMeshGroups(ignoreMeshGroups);
                AssetBuilder.SetSkipMeshGrouping(skipMeshGrouping);

                RebuildAsync(forced: true).Forget();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

#region Composition
        public void AddEntity(Entity entity)
        {
            AssetBuilder.AddEntity(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            AssetBuilder.RemoveEntity(entity);
        }

        public void RemoveEntity(string assetId)
        {
            using VectorEntity entities = AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                if (!entity.Name().Contains(assetId))
                {
                    continue;
                }

                AssetBuilder.RemoveEntity(entity);
                return;
            }
        }

        public void AddEntities(IEnumerable<Entity> entities, bool disposeAfterAdding = false)
        {
            foreach (Entity entity in entities)
            {
                if (entity is null)
                {
                    continue;
                }

                if (!entity.IsNull())
                {
                    AssetBuilder.AddEntity(entity);
                }

                if (disposeAfterAdding)
                {
                    entity.Dispose();
                }
            }
        }

        public void RemoveEntities(IEnumerable<Entity> entities, bool disposeAfterRemoving = false)
        {
            foreach (Entity entity in entities)
            {
                if (entity is null)
                {
                    continue;
                }

                if (!entity.IsNull())
                {
                    AssetBuilder.RemoveEntity(entity);
                }

                if (disposeAfterRemoving)
                {
                    entity.Dispose();
                }
            }
        }

        public bool ContainsEntity(Entity entity)
        {
            return AssetBuilder.ContainsEntity(entity);
        }

        public bool ContainsEntity(string assetId)
        {
            using VectorEntity entities = AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                if (entity.Name().Contains(assetId))
                {
                    return true;
                }
            }

            return false;
        }

        public void ClearEntities()
        {
            AssetBuilder.ClearEntities();
        }

        public async UniTask LoadAndAddCombinableAssetAsync(string assetId, Dictionary<string, string> parameters = null)
        {
            using Entity entity = await LoadCombinableAssetAsync(assetId, parameters);
            if (!entity.IsNull())
            {
                AddEntity(entity);
            }
        }

        public async UniTask LoadAndAddCombinableAssetsAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null)
        {
            Entity[] entities = await LoadCombinableAssetsAsync(assetIds, parameters);
            AddEntities(entities, disposeAfterAdding: true);
        }

        public async UniTask LoadAndAddCombinableAssetsAsync(IEnumerable<AssetLoadRequest> requests)
        {
            Entity[] entities = await LoadCombinableAssetsAsync(requests);
            AddEntities(entities, disposeAfterAdding: true);
        }
#endregion

#region ColorEditor
        public void SetColor(string id, Color color)
        {
            // set the color to the color editor, which will update the runtime mesh primitive materials
            ColorEditor.SetColor(id, color.r, color.g, color.b, color.a);
        }

        public void UnsetColor(string id)
        {
            ColorEditor.UnsetColor(id);
        }

        public void UnsetAllColors()
        {
            ColorEditor.UnsetAllColors();
        }

        public Color? GetColor(string id)
        {
            IntPtr colorPtr = ColorEditor.GetColor(id);
            if (colorPtr == IntPtr.Zero)
            {
                return null;
            }

            return Marshal.PtrToStructure<Color>(colorPtr);
        }

        public bool ColorAttributeExists(string id)
        {
            return ColorEditor.AttributeExists(id);
        }

        public List<string> GetExistingColorAttributes()
        {
            using VectorString attributes = ColorEditor.GetExistingAttributes();
            return new List<string>(attributes);
        }

        public void AddExistingColorAttributes(ICollection<string> result)
        {
            using VectorString attributes = ColorEditor.GetExistingAttributes();
            foreach (string attribute in attributes)
            {
                result.Add(attribute);
            }
        }
#endregion

#region ShapeEditor
        public void SetShapeAttributeWeight(string id, float weight)
        {
            ShapeEditor.SetAttributeWeight(id, weight);
        }

        public void ResetShapeAttributeWeights()
        {
            ShapeEditor.ResetAttributeWeights();
        }

        public void SetShapeAttributes(BodyAttributesPreset preset)
        {
            foreach (BodyAttributeState state in preset.attributesStates)
            {
                ShapeEditor.SetAttributeWeight(state.name, state.weight);
            }
        }

        public bool ShapeAttributeExists(string id)
        {
            return ShapeEditor.AttributeExists(id);
        }

        public float GetShapeAttributeWeight(string id)
        {
            return ShapeEditor.GetAttributeWeight(id);
        }

        public List<string> GetExistingShapeAttributes()
        {
            using VectorString attributes = ShapeEditor.GetExistingAttributes();
            return new List<string>(attributes);
        }

        public void AddExistingShapeAttributes(ICollection<string> result)
        {
            using VectorString attributes = ShapeEditor.GetExistingAttributes();
            foreach (string attribute in attributes)
            {
                result.Add(attribute);
            }
        }
#endregion

#region TattooEditor
        public void SetTattoo(MegaSkinTattooSlot slot, GnWrappers.Texture texture)
        {
            TattooEditor.SetTattoo(slot, texture);
        }

        public async UniTask LoadAndSetTattooAsync(MegaSkinTattooSlot slot, string assetId, Dictionary<string, string> parameters = null)
        {
            using GnWrappers.Texture texture = await LoadTextureAsync(assetId, parameters);
            if (!texture.IsNull())
            {
                SetTattoo(slot, texture);
            }
        }

        public void UnsetTattoo(MegaSkinTattooSlot slot)
        {
            TattooEditor.UnsetTattoo(slot);
        }

        public void UnsetAllTattoos()
        {
            TattooEditor.UnsetAllTattoos();
        }

        public GnWrappers.Texture GetTattoo(MegaSkinTattooSlot slot)
        {
            return TattooEditor.GetTattoo(slot);
        }

        public bool IsTattooEquipped(MegaSkinTattooSlot slot, string assetId)
        {
            using GnWrappers.Texture tattoo = TattooEditor.GetTattoo(slot);
            return !tattoo.IsNull() && tattoo.Name() == assetId;
        }
#endregion

#region Multithreading
    public void CancelAsyncLoad(int handle)
    {
        _containerApi.CancelAsyncLoad(handle);
    }

    public void CancelAsyncLoads()
    {
        _containerApi.CancelAllAsyncLoads();
    }

    public void CancelTextureAsyncLoad(int handle)
    {
        _containerApi.CancelTextureAsyncLoad(handle);
    }

    public void CancelTextureAsyncLoads()
    {
        _containerApi.CancelAllTextureAsyncLoads();
    }

    public void CancelIconAsyncLoad(int handle)
    {
        _containerApi.CancelIconAsyncLoad(handle);
    }

    public void CancelIconAsyncLoads()
    {
        _containerApi.CancelAllIconAsyncLoads();
    }
#endregion

#region LoadingApi
        public async UniTask<Entity> LoadCombinableAssetAsync(string assetId, UnorderedMapString cParams = null)
        {
            int handle = _containerApi.LoadCombinableAssetAsync(assetId, cParams);
            if (handle == -1)
            {
                return null;
            }

            while (!_containerApi.IsAsyncLoadComplete(handle))
            {
                await UniTask.Yield();
            }

            return _containerApi.GetAsyncLoadResult(handle);
        }

        public UniTask<Entity> LoadCombinableAssetAsync(string assetId, Dictionary<string, string> parameters = null)
        {
            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            return LoadCombinableAssetAsync(assetId, cParams);
        }

        public UniTask<Entity> LoadCombinableAssetAsync(AssetLoadRequest request)
        {
            return LoadCombinableAssetAsync(request.assetId, request.parameters);
        }

        public UniTask<Entity[]> LoadCombinableAssetsAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null)
        {
            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            return UniTask.WhenAll(assetIds.Select(id => LoadCombinableAssetAsync(id, cParams)));
        }

        public UniTask<Entity[]> LoadCombinableAssetsAsync(IEnumerable<AssetLoadRequest> loadRequests)
        {
            return UniTask.WhenAll(loadRequests.Select(LoadCombinableAssetAsync));
        }

        public async UniTask<GnWrappers.Texture> LoadTextureAsync(string assetId, UnorderedMapString cParams = null)
        {
            int handle = _containerApi.LoadTextureAsync(assetId, cParams);
            if (handle == -1)
            {
                return null;
            }

            while (!_containerApi.IsTextureAsyncLoadComplete(handle))
            {
                await UniTask.Yield();
            }

            return _containerApi.GetTextureAsyncLoadResult(handle);
        }

        public UniTask<GnWrappers.Texture> LoadTextureAsync(string assetId, Dictionary<string, string> parameters = null)
        {
            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            return LoadTextureAsync(assetId, cParams);
        }

        public UniTask<GnWrappers.Texture> LoadTextureAsync(AssetLoadRequest request)
        {
            return LoadTextureAsync(request.assetId, request.parameters);
        }

        public UniTask<GnWrappers.Texture[]> LoadTexturesAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null)
        {
            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            return UniTask.WhenAll(assetIds.Select(id => LoadTextureAsync(id, cParams)));
        }

        public UniTask<GnWrappers.Texture[]> LoadTexturesAsync(IEnumerable<AssetLoadRequest> loadRequests)
        {
            return UniTask.WhenAll(loadRequests.Select(LoadTextureAsync));
        }
#endregion

        public async UniTask RebuildAsync(bool forced = false)
        {
            // rebuild the composition on the thread pool so we don't block the main thread
            await UniTask.RunOnThreadPool(() => AssetBuilder.Build(forced));

            using Entity result = AssetBuilder.Result();

            if (result.IsNull())
            {
                _nativeGenie.ClearGenie();
                return;
            }

            // if the rebuild is forced, use the SetGenie method which will forcefully rebuild everything
            if (!RefittingDebuggerEnabled && forced)
            {
                _nativeGenie.SetGenie(result);
                return;
            }

            // begin editing the native genie (this will ensure that rebuilt events are not triggered until the end)
            _nativeGenie.BeginEditing();

            try
            {
                // rebuild the mesh, skeleton, materials and blendshapes based on the RCM reports (forced = false)
                _nativeGenie.MeshBuilder.RebuildMesh(result, forced: false);

                // always rebuild the skeleton offset (this should be a cheap operation)
                RebuildSkeletonOffset();

                // always rebuild tattoos too (also should be cheap)
                RebuildTattoos();

                // set the extras (NativeGenie already implements extras checking to be efficient, so no need for reports here), also the extras don't trigger any rebuilt events
                if (RefittingDebuggerEnabled)
                {
                    _nativeGenie.ClearExtras();
                }
                else
                {
                    using EntityExtras extras = EntityExtras.GetFrom(result);
                    _nativeGenie.SetExtras(extras);
                }
            }
            finally
            {
                // end editing so the rebuilt events are triggered
                _nativeGenie.EndEditing();
            }
        }

        /**
         * You must call this after setting colors. Unsetting colors though require a full rebuild so this method won't work.
         */
        public void RebuildColors()
        {
            // fetch the current runtime mesh
            using Entity result = AssetBuilder.Result();
            using NativeMultiMesh nativeMultiMesh = new NativeMultiMesh(result);
            IReadOnlyList<SkinnedNativeMeshRenderer> nativeRenderers = _nativeGenie.MeshBuilder.NativeRenderers;

            for (int i = 0; i < nativeMultiMesh.Meshes.Count; ++i)
            {
                if (i >= nativeRenderers.Count)
                {
                    break;
                }

                NativeMultiMesh.Mesh mesh = nativeMultiMesh.Meshes[i];
                if (mesh.RuntimeMesh.IsNull())
                {
                    Debug.LogError($"RebuildColors: runtime mesh is null for native mesh at index {i}.");
                    break;
                }

                SkinnedNativeMeshRenderer renderer = nativeRenderers[i];
                if (renderer.name != mesh.Id)
                {
                    Debug.LogError($"RebuildColors: renderer name '{renderer.name}' does not match runtime mesh id '{mesh.Id}'.");
                    break;
                }

                RebuildMeshColors(renderer, mesh);
            }

            _nativeGenie.NotifyRebuild();
        }

        public void SetRefittingDebuggerEnabled(bool enabled)
        {
            if (RefittingDebuggerEnabled == enabled)
            {
                return;
            }

            RefittingDebuggerEnabled = enabled;
            if (RefittingDebuggerEnabled)
            {
                AssetBuilder.SetRmcDebugSettings(refDebSkipMeshGrouping, refDebSingleGroup, refDebIncludeNonDeformedMeshes, refDebEnableVertexAttributes);
            }
            else
            {
                AssetBuilder.SetDefaultRmcSettings(!disableRefitting);
            }
        }

        private static void RebuildMeshColors(SkinnedNativeMeshRenderer renderer, NativeMultiMesh.Mesh mesh)
        {
            /**
             * Here we could just do "renderer.SetMaterials(mesh.RuntimeMesh);" and it would work relatively fine. But
             * since we know we just want to update the colors, this will be more efficient. Doing some profiling on my
             * desktop computer shows a 6x performance, but still we are talking about less than 1 ms for the
             * SetMaterials approach, so this is not a big deal.
             */

            // iterate over all the primitives and set their colors (vector4 properties) to the current NativeMaterial instances in the renderer
            IReadOnlyList<NativeMaterial> materials = renderer.Materials;
            uint primitiveCount = mesh.RuntimeMesh.PrimitiveCount();
            for (uint primitiveIndex = 0; primitiveIndex < primitiveCount && primitiveIndex < materials.Count; ++primitiveIndex)
            {
                using Primitive primitive = mesh.RuntimeMesh.GetPrimitive(primitiveIndex);
                using GnWrappers.Material material = primitive.Material();
                if (material is null)
                {
                    continue;
                }

                NativeMaterial nativeMaterial = materials[(int)primitiveIndex];
                if (!nativeMaterial.Material)
                {
                    continue;
                }

                nativeMaterial.AddVector4Properties(material);
            }
        }

        /**
         * You must call this after modifying shape attributes.
         */
        public void RebuildSkeletonOffset()
        {
            using Entity result = AssetBuilder.Result();
            using SkeletonOffset skeletonOffset = SkeletonOffset.GetFrom(result);
            _nativeGenie.SetSkeletonOffset(skeletonOffset);
        }

        /**
         * You must call this after modifying tattoos.
         */
        public void RebuildTattoos()
        {
            using Entity result = AssetBuilder.Result();
            using MegaSkinTattoos tattoos = MegaSkinTattoos.GetFrom(result);
            _nativeGenie.SetTattoos(tattoos);
        }

        public void Dispose()
        {
            // use the container api as a flag for disposal
            if (_containerApi is null)
            {
                return;
            }

            // the NativeGenie should not be disposed here. It will dispose itself if destroyed

            ColorEditor?.Dispose();
            ShapeEditor?.Dispose();
            TattooEditor?.Dispose();
            AssetBuilder?.Dispose();
            _containerApi?.Dispose();

            ColorEditor   = null;
            ShapeEditor   = null;
            TattooEditor  = null;
            AssetBuilder  = null;
            _containerApi = null;

            /**
             * Since the NativeGenieBuilder is a component, we destroy it automatically on dispose. The GameObject
             * should never be destroyed here.
             */
            if (this)
            {
                Destroy(this);
            }
        }

        [ContextMenu("Rebuild")] private void MenuRebuild() => RebuildAsync(forced: false).Forget();
        [ContextMenu("Forced Rebuild")] private void MenuForcedRebuild() => RebuildAsync(forced: true).Forget();
    }
}
