using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;
using Genies.Utilities;
using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class EditableGenie : MonoBehaviour, IEditableGenie
    {
        // inspector
        [SerializeField] private Animator animator;
        [SerializeField] private new SkinnedMeshRenderer renderer;
        [SerializeField] private List<GenieComponentAsset> componentAssets = new();

        [Space(16), Header("Mesh Builder Settings")]
        public bool dontMergeAssets;
        public TextureSettings atlasSettings;
        public bool useMeshSurfaceAtlasOptimization;
        public SurfacePixelDensity surfacePixelDensity = new()
        {
            targetDensity = 1024.0f,
            minPixelArea = 0,
            maxPixelArea = 1024,
            snappingMethod = ValueSnapping.Method.None,
        };

        // properties
        public string                              GenieType    => GenieTypeName.NonUma;
        public string                              Lod          => AssetLod.Default;
        public GameObject                          Root         => gameObject;
        public GameObject                          ModelRoot    { get; private set; }
        public Transform                           SkeletonRoot { get; private set; }
        public string                              Species      { get; private set; }
        public string                              SubSpecies   { get; private set; }
        public Animator                            Animator     => animator;
        public IReadOnlyList<SkinnedMeshRenderer>  Renderers    { get; private set; }
        public GenieComponentManager               Components   { get; private set; }
        public IReadOnlyCollection<OutfitAsset>    OutfitAssets => _outfitManager.OutfitAssets;
        public IReadOnlyCollection<IGenieMaterial> Materials    => _materialManager.Materials;
        public bool                                IsDirty      => _materialManager.IsAnyMaterialDirty || _dnaManager.IsDirty || _meshBuilder.IsDirty;
        public bool                                IsDisposed   { get; private set; }

        private Transform _SkinnedBoneParent
        {
            get { return _global != null? _global : SkeletonRoot; }
        }

        /// <summary>
        /// If enabled, any assets generated will be cached for the next rebuild and reused if possible. Any cached
        /// assets not used for a rebuild are released after the rebuild finishes. Disable this if you want combined
        /// materials to be always rebuilt.
        /// </summary>
        public bool UseCache
        {
            get => _meshBuilder.UseCache;
            set => _meshBuilder.UseCache = value;
        }

        // events
        public event Action RootRebuilt;
        public event Action Rebuilt;
        public event Action Disposed;

        // managers
        private EditableGenieOutfitManager _outfitManager;
        private EditableGenieMaterialManager _materialManager;
        private EditableGenieDnaManager _dnaManager;
        private EditableGenieBlendShapesManager _blendShapesManager;
        private EditableGenieSubSpeciesManager _subSpeciesManager;

        // state
        private MeshBuilder _meshBuilder = new();
        private readonly List<SkinnedMeshRenderer> _renderers = new();
        private readonly List<SkeletonBone> _tmpSkeleton = new(256);
        private readonly Dictionary<string, Transform> _bonesByName = new();
        private bool _initialized;
        private Ref<SpeciesAsset> _speciesAssetRef;
        private List<MeshAsset> _baseMeshAssets;
        private UmaTPose _umaTPose;
        private RaceData.UMATarget _umaTarget;
        private Avatar _avatar;
        private SkeletonBone[] _skeleton;
        private Avatar _groundedAvatar;
        private Transform _global;
        private bool _fixupRotations;

        private Ref<SubSpeciesAsset> _subSpeciesAssetRef;
        private bool _usingUnifiedGap => Species.Equals(GenieSpecies.UnifiedGAP);


        private void OnValidate()
        {
            SyncMeshBuilderSettings();
        }

        public UniTask<bool> TryInitializeAsync(Ref<SpeciesAsset> speciesAssetRef)
        {
            if (IsDisposed)
            {
                return UniTask.FromResult(false);
            }

            // allow only one initialization call
            if (_initialized)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] this component can only be initialized once");
                speciesAssetRef.Dispose();
                Dispose();
                return UniTask.FromResult(false);
            }

            // try to set the species
            bool successful = TrySetSpecies(speciesAssetRef);
            if (!successful)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] couldn't set the species");
                Dispose();
                return UniTask.FromResult(false);
            }
            SubSpecies = string.Empty;

            // model root
            Renderers = _renderers.AsReadOnly();
            _renderers.Add(renderer);
            InitializeModelRoot();

            // initialize managers
            _outfitManager = new EditableGenieOutfitManager(this, _meshBuilder);
            _materialManager = new EditableGenieMaterialManager(this, _speciesAssetRef.Item.MappedUmaIdentifiers);
            _dnaManager = new EditableGenieDnaManager(this, _speciesAssetRef.Item.Race.dnaConverterList[0] as DynamicDNAConverterController, renderer);
            _blendShapesManager = new EditableGenieBlendShapesManager(this);

            InitializeComponents();

            // initialization completed successfully
            _initialized = true;
            return UniTask.FromResult(true);
        }

        public async UniTask<bool> TryInitializeWithGAPAsync(Ref<SubSpeciesAsset> subSpeciesAssetRef)
        {
            if (IsDisposed)
            {
                return false;
            }

            // allow only one initialization call
            if (_initialized)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] this component can only be initialized once");
                subSpeciesAssetRef.Dispose();
                Dispose();
                return false;
            }

            // set up subspecies manager with mesh builder
            _subSpeciesManager = new EditableGenieSubSpeciesManager(this, _meshBuilder);

            // try to set the species
            bool successful = await TrySetSubSpecies(subSpeciesAssetRef);
            if (!successful)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] couldn't set the SubSpecies");
                Dispose();
                return false;
            }

            // model root
            Renderers = _renderers.AsReadOnly();
            _renderers.Add(renderer);
            InitializeModelRoot();

            // initialize managers
            _outfitManager = new EditableGenieOutfitManager(this, _meshBuilder);
            _materialManager = new EditableGenieMaterialManager(this, null);
            _dnaManager = new EditableGenieDnaManager(this, subSpeciesAssetRef.Item.BlendshapeNames, renderer);
            _blendShapesManager = new EditableGenieBlendShapesManager(this);

            // initialize components
            InitializeComponents();

            // initialization completed successfully
            _initialized = true;
            return true;
        }

        public UniTask RebuildGAPAsync(bool forceRebuild = false, bool spreadCompute = false)
        {
            if (IsDisposed)
            {
                return UniTask.CompletedTask;
            }

            if (!forceRebuild && !_meshBuilder.IsDirty)
            {
                if (!_materialManager.IsAnyMaterialDirty)
                {
                    _dnaManager.ApplyDnaIfDirty();
                    return UniTask.CompletedTask;
                }

                // if the avatar is not dirty but any materials are, then apply only the materials
                _materialManager.ApplyMaterials();
                _dnaManager.ApplyDnaIfDirty();
                Rebuilt?.Invoke();
                return UniTask.CompletedTask;
            }

            try
            {
                if (SkeletonRoot)
                {
                    DestroyImmediate(SkeletonRoot.gameObject);
                }

                // Set up mesh builder
                SetupMeshBuilder();
                _meshBuilder.Rebuild();

                // Apply new mesh to renderer and rebuild the skeleton hierarchy
                _meshBuilder.ApplyToRenderer(renderer, transform);

                // Apply managers
                _blendShapesManager.ApplyBlendShapes();
                _dnaManager.ApplyDna();

                // Create avatar from GAP HumanDescription
                _avatar = AvatarBuilder.BuildHumanAvatar(Root, _subSpeciesAssetRef.Item.HumanDescription);
                animator.avatar = _avatar;
                SkeletonRoot = Root.transform.Find("Root");
                _global = SkeletonRoot.transform.Find("Global");

                // If this is a humanoid avatar then rebuild
                // the animator's Avatar asset so the genie is properly grounded
                SnapToGroundIfHuman();

                // update renderer bounds with the first frame of the animation
                if (!renderer.updateWhenOffscreen)
                {
                    renderer.updateWhenOffscreen = true;
                    animator.Update(0);
                    Bounds bounds = renderer.localBounds;
                    renderer.updateWhenOffscreen = false;
                    renderer.localBounds = bounds;
                }

            } catch (Exception exception)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] an exception occured while trying to rebuild the GAP avatar.\n{exception}");
            }

            // remove components from unequipped outfit assets and add components from equipped ones
            _outfitManager.RefreshAssetComponents();

            RootRebuilt?.Invoke(); // fire this only when rebuilding the UMA character
            Rebuilt?.Invoke();
            return UniTask.CompletedTask;
        }

        public async UniTask RebuildAsync(bool forceRebuild = false, bool spreadCompute = false)
        {
            if (IsDisposed)
            {
                return;
            }

            if (_usingUnifiedGap)
            {
                await RebuildGAPAsync(forceRebuild, spreadCompute);
                return;
            }

            if (!forceRebuild && !_meshBuilder.IsDirty)
            {
                if (!_materialManager.IsAnyMaterialDirty)
                {
                    _dnaManager.ApplyDnaIfDirty();
                    return;
                }

                // if the avatar is not dirty but any materials are, then apply only the materials
                _materialManager.ApplyMaterials();
                _dnaManager.ApplyDnaIfDirty();
                Rebuilt?.Invoke();
                return;
            }

            // build the UMA avatar
            try
            {
                // rebuild mesh
                RebuildSkeletonRoot();
                SetupMeshBuilder();
                if (spreadCompute)
                {
                    await _meshBuilder.RebuildOverFrames();
                }
                else
                {
                    _meshBuilder.Rebuild();
                }

                // apply new mesh to renderer and rebuild the skeleton hierarchy
                _meshBuilder.ApplyToRenderer(renderer, _SkinnedBoneParent);

                // apply managers and build the humanoid Avatar asset
                _materialManager.RefreshSlots();
                _materialManager.ApplyMaterials(forced: true);
                _blendShapesManager.ApplyBlendShapes();
                _dnaManager.ApplyDna();
                RebuildAvatar();

                // if this is a humanoid avatar then rebuild the animator's Avatar asset so the genie is properly grounded
                SnapToGroundIfHuman();

                // update renderer bounds with the first frame of the animation
                if (!renderer.updateWhenOffscreen)
                {
                    renderer.updateWhenOffscreen = true;
                    animator.Update(0);
                    Bounds bounds = renderer.localBounds;
                    renderer.updateWhenOffscreen = false;
                    renderer.localBounds = bounds;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] an exception occured while trying to rebuild the avatar.\n{exception}");
            }

            // remove components from unequipped outfit assets and add components from equipped ones
            _outfitManager.RefreshAssetComponents();

            RootRebuilt?.Invoke(); // fire this only when rebuilding the UMA character
            Rebuilt?.Invoke();
        }

        public async UniTask<IGenie> CloneAsync(int onLayer = -1)
        {
            IGenie genie = IsDisposed ? null : await CloneGenie.CreateAsync(this, onLayer);
            return genie;
        }

        public UniTask<IGenie> BakeAsync(Transform parent = null, bool urpBake = false)
        {
            return UniTask.FromException<IGenie>(new NotImplementedException());
        }

        public UniTask<IGenieSnapshot> TakeSnapshotAsync(Transform parent = null, bool urpBake = false)
        {
            throw new NotImplementedException();
        }

        public void EnableAnimation()
        {
            RebuildSkeletonRoot();
            renderer.bones = _meshBuilder.CreateSkeletonHierarchy(_SkinnedBoneParent);
            animator.enabled = true;
            RebuildAvatar();
        }

        public void DisableAnimation()
        {
            animator.enabled = false;
            RebuildSkeletonRoot();
            renderer.bones = _meshBuilder.CreateSkeletonHierarchy(_SkinnedBoneParent);
            SkeletonRoot.Rotate(new Vector3(90.0f, 0.0f, 90.0f));
        }

        public void Dispose()
        {
            if (!_initialized)
            {
                return;
            }

            IsDisposed = true;
            Components?.RemoveAll();
            Components = null;
            _subSpeciesManager?.Dispose();
            _outfitManager?.Dispose();
            _materialManager?.Dispose();
            _speciesAssetRef.Dispose();
            _subSpeciesAssetRef.Dispose();
            _meshBuilder?.Dispose();
            _blendShapesManager?.Dispose();
            _dnaManager?.Dispose();

            foreach (var r in _renderers)
            {
                Destroy(r);
            }

            _renderers.Clear();
            _tmpSkeleton.Clear();
            _bonesByName.Clear();

            if (_baseMeshAssets is not null)
            {
                _baseMeshAssets.Clear();
            }

            if (gameObject)
            {
                Destroy(gameObject);
            }

            if (_groundedAvatar)
            {
                Destroy(_groundedAvatar);
            }

            Disposed?.Invoke();
        }

        public void DisposeOnDestroy()
        {
            // mesh builder fully dispose
            _meshBuilder?.DisposeOnDestroy();
            Components?.RemoveAll();
            Components = null;
            _subSpeciesManager?.Dispose();
            _outfitManager?.Dispose();
            _materialManager?.Dispose();
            _speciesAssetRef.Dispose();
            _blendShapesManager?.Dispose();
            _dnaManager?.Dispose();

            foreach (var r in _renderers)
            {
                Destroy(r);
            }

            _renderers.Clear();
            _tmpSkeleton.Clear();
            _bonesByName.Clear();

            if (_baseMeshAssets is not null && !_usingUnifiedGap)
            {
                _baseMeshAssets.Clear();
            }


            // set references to null
            _meshBuilder = null;
            _baseMeshAssets = null;

            if (gameObject)
            {
                Destroy(gameObject);
            }

            if (_groundedAvatar)
            {
                Destroy(_groundedAvatar);
            }

            Disposed?.Invoke();
        }

        private void RebuildSkeletonRoot()
        {
            if (SkeletonRoot)
            {
                DestroyImmediate(SkeletonRoot.gameObject);
            }

            SkeletonRoot = new GameObject("Root").transform;
            SkeletonRoot.SetParent(transform, worldPositionStays: false);
            if (_fixupRotations)
            {
                SkeletonRoot.localRotation = Quaternion.Euler(270.0f, 0.0f, 0.0f);
            }
            else
            {
                SkeletonRoot.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }

            if (!HasGlobal(_umaTPose))
            {
                return;
            }

            _global = new GameObject("Global").transform;
            _global.SetParent(SkeletonRoot, worldPositionStays: false);

            if (_fixupRotations)
            {
                _global.localRotation = Quaternion.Euler(90.0f, 90.0f, 0.0f);
            }
            else
            {
                _global.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            }
        }

        private void SetupMeshBuilder()
        {
            SyncMeshBuilderSettings();

            _meshBuilder.UnhideAllTriangles();
            foreach (OutfitAsset asset in _outfitManager.OutfitAssets)
            {
                _meshBuilder.HideTriangles(asset.HiddenTriangles);
            }
        }

        private void SyncMeshBuilderSettings()
        {
            _meshBuilder.DontMergeAssets = dontMergeAssets;
            _meshBuilder.TextureSettings = atlasSettings;
            _meshBuilder.UseMeshSurfaceAtlasOptimization = useMeshSurfaceAtlasOptimization;
            _meshBuilder.SurfacePixelDensity = surfacePixelDensity;
        }

        private void RebuildAvatar()
        {
            if (_avatar)
            {
                Destroy(_avatar);
            }

            _avatar = null;

            if (_umaTarget is not RaceData.UMATarget.Humanoid)
            {
                return;
            }

            RebuildSkeleton();
            var humanDescription = new HumanDescription
            {
                human         = _umaTPose.humanInfo,
                skeleton      = _skeleton,
                feetSpacing   = _umaTPose.feetSpacing,
                armStretch    = _umaTPose.armStretch    == 0.0f ? 0.05f : _umaTPose.armStretch,
                legStretch    = _umaTPose.legStretch    == 0.0f ? 0.05f : _umaTPose.legStretch,
                lowerArmTwist = _umaTPose.lowerArmTwist == 0.0f ? 0.50f : _umaTPose.lowerArmTwist,
                lowerLegTwist = _umaTPose.lowerLegTwist == 0.0f ? 0.50f : _umaTPose.lowerLegTwist,
                upperArmTwist = _umaTPose.upperArmTwist == 0.0f ? 0.50f : _umaTPose.upperArmTwist,
                upperLegTwist = _umaTPose.upperLegTwist == 0.0f ? 0.50f : _umaTPose.upperLegTwist,
            };

            _avatar = AvatarBuilder.BuildHumanAvatar(Root, humanDescription);
            animator.avatar = _avatar;
        }

        private void RebuildSkeleton()
        {
            _tmpSkeleton.Clear();

            // add current genie root bone
            var rootBone = new SkeletonBone
            {
                name     = Root.name,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                scale    = Vector3.one,
            };

            _tmpSkeleton.Add(rootBone);

            // get current bones by name
            _bonesByName.Clear();
            SkeletonRoot.AddChildrenByName(_bonesByName, includeSelf: true);

            for (int i = 0; i < _umaTPose.boneInfo.Length; ++i)
            {
                // only add bones existing in the current built skeleton hierarchy
                if (_bonesByName.ContainsKey(_umaTPose.boneInfo[i].name))
                {
                    _tmpSkeleton.Add(_umaTPose.boneInfo[i]);
                }
            }

            // if the current skeleton array has the same size then keep it to avoid unnecessary heap allocation
            if (_skeleton is null || _skeleton.Length != _tmpSkeleton.Count)
            {
                _skeleton = new SkeletonBone[_tmpSkeleton.Count];
            }

            for (int i = 0; i < _skeleton.Length; ++i)
            {
                _skeleton[i] = _tmpSkeleton[i];
            }

            _tmpSkeleton.Clear();
            _bonesByName.Clear();
        }

        private void OnDestroy()
        {
            DisposeOnDestroy();
        }

        private bool TrySetSpecies(Ref<SpeciesAsset> speciesAssetRef)
        {
            if (!speciesAssetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] the given species asset ref is dead");
                return false;
            }

            if (speciesAssetRef.Item.Race.baseRaceRecipe is not UMATextRecipe recipe)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] the given species asset UMA recipe is not a UMATextRecipe");
                return false;
            }

            _speciesAssetRef = speciesAssetRef;
            Species = _speciesAssetRef.Item.Id;
            _umaTarget = _speciesAssetRef.Item.Race.umaTarget;
            _fixupRotations = _speciesAssetRef.Item.Race.FixupRotations;

            try
            {
                _baseMeshAssets = MeshAssetUtility.CreateMeshAssetsFrom(_speciesAssetRef.Item);
                _meshBuilder.Add(_baseMeshAssets);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] an exception occured while trying to add the base mesh assets to the builder.\n{exception}");
                return false;
            }

            try
            {
                _umaTPose = _speciesAssetRef.Item.Race.TPose;
                _umaTPose.DeSerialize();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] an exception occured while trying to deserialize the UMA T pose from the species asset.\n{exception}");
                return false;
            }

            return true;
        }

        private async UniTask<bool> TrySetSubSpecies(Ref<SubSpeciesAsset> subSpeciesAssetRef)
        {
            if (!subSpeciesAssetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(EditableGenie)}] the given species asset ref is dead");
                return false;
            }

            // dispose of previous ref
            _subSpeciesAssetRef.Dispose();

            _subSpeciesAssetRef = subSpeciesAssetRef;
            Species = _subSpeciesAssetRef.Item.Species;
            SubSpecies = _subSpeciesAssetRef.Item.Id;
            _umaTarget = _subSpeciesAssetRef.Item.Target;
            _fixupRotations = true;

            // equip sub species via manager
            await _subSpeciesManager.EquipSubSpecies(_subSpeciesAssetRef.Item);
            _baseMeshAssets = _subSpeciesAssetRef.Item.MeshAssets;


            return true;
        }


        private void InitializeModelRoot()
        {
            // if we have only one renderer then make it the model root
            if (_renderers.Count == 1)
            {
                ModelRoot = _renderers[0].gameObject;
                _renderers[0].name = "Model";
                return;
            }

            // for multiple renderers create the model root GO and parent all renderers to it
            ModelRoot = new GameObject("Model");
            ModelRoot.transform.SetParent(transform, worldPositionStays: false);

            foreach (SkinnedMeshRenderer renderer in _renderers)
            {
                renderer.transform.SetParent(ModelRoot.transform, worldPositionStays: true);
            }
        }

        private void InitializeComponents()
        {
            Components = new GenieComponentManager(this);

            var assetComponentCreators = _usingUnifiedGap
                ? _subSpeciesAssetRef.Item.Components
                : _speciesAssetRef.Item.ComponentCreators;

            var componentCreators = new List<IGenieComponentCreator>(componentAssets.Count + assetComponentCreators.Length);            componentCreators.AddRange(componentAssets);
            componentCreators.AddRange(assetComponentCreators);

            // add components on the first rebuild
            Rebuilt += AddComponents;
            return;

            void AddComponents()
            {
                Rebuilt -= AddComponents;

                foreach (IGenieComponentCreator componentCreator in componentCreators)
                {
                    GenieComponent component = componentCreator?.CreateComponent();
                    if (component is not null)
                    {
                        Components.Add(component);
                    }
                }
            }
        }

        private void SnapToGroundIfHuman()
        {
            if (!Animator.isHuman || !Animator.avatar)
            {
                return;
            }

            Avatar newGroundedAvatar = GenieAvatarBuilder.BuildGroundedHumanAvatar(this);

            if (Animator.avatar)
            {
                Destroy(Animator.avatar);
            }

            Animator.avatar = newGroundedAvatar;

            if (_groundedAvatar)
            {
                Destroy(_groundedAvatar);
            }

            _groundedAvatar = newGroundedAvatar;
        }

        private bool HasGlobal(UmaTPose tpose)
        {
            foreach (var bone in tpose.boneInfo)
            {
                if(bone.name == "Global")
                {
                    return true;
                }
            }

            return false;
        }


#region MANAGER_WRAPPERS
        public UniTask AddOutfitAssetAsync(OutfitAsset asset)
            => _outfitManager.AddOutfitAssetAsync(asset);

        public UniTask RemoveOutfitAssetAsync(OutfitAsset asset)
            => _outfitManager.RemoveOutfitAssetAsync(asset);

        public void AddOutfitAssetProcessor(IOutfitAssetProcessor processor)
            => _outfitManager.AddOutfitAssetProcessor(processor);

        public void RemoveOutfitAssetProcessor(IOutfitAssetProcessor processor)
            => _outfitManager.RemoveOutfitAssetProcessor(processor);

        public void AddMaterial(IGenieMaterial material)
            => _materialManager.AddMaterial(material);

        public void RemoveMaterial(IGenieMaterial material)
            => _materialManager.RemoveMaterial(material);

        public void ClearMaterialSlot(string slotId)
            => _materialManager.ClearMaterialSlot(slotId);

        public void RemoveMaterial(string slot)
            => _materialManager.ClearMaterialSlot(slot);

        public bool TryGetMaterial(string slotId, out IGenieMaterial material)
            => _materialManager.TryGetMaterial(slotId, out material);

        public bool TryGetSharedMaterial(string slotId, out Material material)
            => _materialManager.TryGetSharedMaterial(slotId, out material);

        public bool SetDna(string name, float value)
            => _dnaManager.SetDna(name, value);

        public float GetDna(string name)
            => _dnaManager.GetDna(name);

        public bool ContainsDna(string name)
            => _dnaManager.ContainsDna(name);

        public void SetBlendShape(string name, float value)
            => _blendShapesManager.SetBlendShape(name, value);

        public void SetBlendShape(string name, float value, bool baked)
            => _blendShapesManager.SetBlendShape(name, value, baked);

        public float GetBlendShape(string name)
            => _blendShapesManager.GetBlendShape(name);

        public bool RemoveBlendShape(string name)
            => _blendShapesManager.RemoveBlendShape(name);

        public bool IsBlendShapeBaked(string name)
            => _blendShapesManager.IsBlendShapeBaked(name);

        public bool ContainsBlendShape(string name)
            => _blendShapesManager.ContainsBlendShape(name);

        public async UniTask EquipSubSpeciesAsync(Ref<SubSpeciesAsset> asset)
        {
            await TrySetSubSpecies(asset);
            await _outfitManager.ReApplyOutfitAssets();
        }

        public Mesh GetEquippedDeformMesh()
            => _subSpeciesManager.EquippedDeformMesh;
#endregion

        [Serializable]
        public struct MaterialBakerLod
        {
            public string lod;
            public MaterialBaker baker;
        }
    }
}
