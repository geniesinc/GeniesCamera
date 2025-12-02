using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.Utilities;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Genies.Avatars
{
    // TODO - Just remove this whole class.
    //[Obsolete("Use NonUmaGenie instead.")
    
    /// <summary>
    /// Component implementation for <see cref="IEditableGenie"/> that requires a <see cref="DynamicCharacterAvatar"/> component from the UMA package.
    /// This is point of abstraction between our tech and UMA and it contains all low level features for customizing and building an avatar.
    /// </summary>
    [RequireComponent(typeof(DynamicCharacterAvatar))]
    public sealed class UmaGenie : MonoBehaviour, IEditableGenie
    {
        // inspector
        [SerializeField]
        private List<MaterialBakerLod> megaSimpleBakers;
        [SerializeField]
        private List<MaterialBakerLod> urpBakers;
        [SerializeField]
        private List<GenieComponentAsset> componentAssets = new();

        // properties
        public DynamicCharacterAvatar              Avatar       { get; private set; }
        public string                              Lod          { get; private set; }
        public GameObject                          Root         => gameObject;
        public GameObject                          ModelRoot    { get; private set; }
        public Transform                           SkeletonRoot { get; private set; }
        public string                              Species      => _speciesAssetRef.Item?.Id;
        public string                              SubSpecies   => string.Empty;
        public Animator                            Animator     { get; private set; }
        public IReadOnlyList<SkinnedMeshRenderer>  Renderers    { get; private set; }
        public GenieComponentManager               Components   { get; private set; }
        public IReadOnlyCollection<OutfitAsset>    OutfitAssets => _outfitManager.OutfitAssets;
        public IReadOnlyCollection<IGenieMaterial> Materials    => _materialManager.Materials;
        public bool                                IsDirty      => _isAvatarDirty || _materialManager.IsAnyMaterialDirty;
        public bool                                IsDisposed   { get; private set; }

        // events
        public event Action RootRebuilt;
        public event Action Rebuilt;
        public event Action Disposed;

        // managers
        private UmaGenieOutfitManager _outfitManager;
        private UmaGenieMaterialManager _materialManager;
        private UmaGenieDnaManager _dnaManager;
        private UmaGenieBlendShapesManager _blendShapesManager;

        // state
        private readonly List<SkinnedMeshRenderer> _renderers = new();
        private bool _initialized;
        private bool _isAvatarDirty;
        private Ref<SpeciesAsset> _speciesAssetRef;
        private GenieBaker _genieBaker;
        private UniTaskCompletionSource _umaBuildOperation;
        private Avatar _groundedAvatar;

        public async UniTask<bool> TryInitializeAsync(string lod, Ref<SpeciesAsset> speciesAssetRef)
        {
            if (IsDisposed)
            {
                return false;
            }

            // allow only one initialization call
            if (_initialized)
            {
                Debug.LogError($"[{nameof(UmaGenie)}] this component can only be initialized once");
                speciesAssetRef.Dispose();
                Dispose();
                return false;
            }

            // initialize
            Lod = lod;
            Avatar = gameObject.GetComponent<DynamicCharacterAvatar>();

            if (!Avatar)
            {
                Debug.LogError($"[{nameof(UmaGenie)}] this component requires a {nameof(DynamicCharacterAvatar)} component");
                speciesAssetRef.Dispose();
                Dispose();
                return false;
            }

            _initialized = true;

            MaterialBaker megaSimpleBaker = GetBakerForLod(lod, megaSimpleBakers);
            MaterialBaker urpBaker = GetBakerForLod(lod, urpBakers);
            _genieBaker = new GenieBaker(lod, megaSimpleBaker, urpBaker);

            Animator = Avatar.GetComponent<Animator>();

            // setup some important settings for the UMA avatar (they should be already set but just in case)
            Avatar.BuildCharacterEnabled = false;
            Avatar.loadBlendShapes = true;

            // initialize state
            _isAvatarDirty = false;

            // try to set the species
            bool successful = await TrySetSpeciesAsync(speciesAssetRef);
            if (!successful)
            {
                Debug.LogError($"[{nameof(UmaGenie)}] couldn't set the species");
                Dispose();
                return false;
            }

            // wait until UMA has initialized and the skinned mesh renderer has been instantiated (timeout after 5 seconds)
            var cancellationSource = new CancellationTokenSource();
            cancellationSource.CancelAfterSlim(TimeSpan.FromSeconds(5.0f));
            await UniTask.WaitWhile(() => !GetComponentInChildren<SkinnedMeshRenderer>(), cancellationToken: cancellationSource.Token);
            Renderers = _renderers.AsReadOnly();
            _renderers.AddRange(Avatar.umaData.GetRenderers());
            InitializeModelRoot();

            // initialize managers
            _outfitManager = new UmaGenieOutfitManager(this);
            _materialManager = new UmaGenieMaterialManager(this, _speciesAssetRef.Item.MappedUmaIdentifiers);
            _dnaManager = new UmaGenieDnaManager(this);
            _blendShapesManager = new UmaGenieBlendShapesManager(this);

            InitializeComponents();

            // subscribe to events
            Avatar.CharacterUpdated.AddListener(OnCharacterUpdated);

            OnCharacterUpdated(Avatar.umaData);

            return true;
        }

        public async UniTask RebuildAsync(bool forceRebuild = false, bool spreadCompute = false)
        {
            if (IsDisposed)
            {
                return;
            }

            if (!_isAvatarDirty && !forceRebuild)
            {
                if (!_materialManager.IsAnyMaterialDirty)
                {
                    return;
                }

                // if the avatar is not dirty but any materials are, then apply only the materials
                _materialManager.ApplyMaterials();
                Rebuilt?.Invoke();
                return;
            }

            // build the UMA avatar
            try
            {
                _umaBuildOperation = new UniTaskCompletionSource();
                Avatar.BuildCharacter();

                // we are subscribed to the UMA character updated event and will finish the build operation when fired
                await _umaBuildOperation.Task;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UmaGenie)}] an exception occured while trying to rebuild the UMA avatar.\n{exception}");
                _umaBuildOperation.TrySetResult();
                _umaBuildOperation = null;
            }

            // remove components from unequipped outfit assets and add components from equipped ones
            _outfitManager.RefreshAssetComponents();

            _isAvatarDirty = false;
            RootRebuilt?.Invoke(); // fire this only when rebuilding the UMA character
            Rebuilt?.Invoke();
        }

        public async UniTask<IGenie> CloneAsync(int onLayer = -1)
        {
            IGenie genie = IsDisposed ? null : await CloneGenie.CreateAsync(this, onLayer);
            return genie;
        }

        public async UniTask<IGenie> BakeAsync(Transform parent = null, bool urpBake = false)
        {
            return await _genieBaker.BakeAsync(this, parent, urpBake);
        }

        public UniTask<IGenieSnapshot> TakeSnapshotAsync(Transform parent = null, bool urpBake = false)
        {
            return _genieBaker.TakeSnapshotAsync(this, parent, urpBake);
        }

        /// <summary>
        /// Will mark the <see cref="Avatar"/> instance for a rebuild.
        /// </summary>
        public void SetUmaAvatarDirty()
        {
            _isAvatarDirty = true;
        }

        public void Dispose()
        {
            if (!_initialized || IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            Components?.RemoveAll();
            Components = null;
            _outfitManager?.Dispose();
            _materialManager?.Dispose();
            _dnaManager?.Dispose();
            _blendShapesManager?.Dispose();
            _speciesAssetRef.Dispose();

            if (Avatar)
            {
                Avatar.CharacterUpdated.RemoveListener(OnCharacterUpdated);
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

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnCharacterUpdated(UMAData umaData)
        {
            // refresh skeleton root
            SkeletonRoot = Avatar.umaData.umaRoot.transform;

            // refresh active slots so the material manager knows what materials are currently present in the renderer
            _materialManager.RefreshSlotActiveStates();
            // always do a forced re-apply of all materials (including non-dirty ones) when the UMA avatar has been rebuilt
            // since UMA resets all the renderer materials on every character build
            _materialManager.ApplyMaterials(forced: true);

            // if this is a humanoid avatar then rebuild the animator's Avatar asset so the genie is properly grounded
            SnapToGroundIfHuman();

            // finish the build operation if it was initialized
            if (_umaBuildOperation is null)
            {
                return;
            }

            UniTaskCompletionSource operation = _umaBuildOperation;
            _umaBuildOperation = null;
            operation.TrySetResult();
        }

        private async UniTask<bool> TrySetSpeciesAsync(Ref<SpeciesAsset> speciesAssetRef)
        {
            if (!speciesAssetRef.IsAlive)
            {
                Debug.LogError($"[{nameof(UMAGenerator)}] the given species asset ref is dead");
                return false;
            }

            _speciesAssetRef.Dispose();
            _speciesAssetRef = speciesAssetRef;
            Avatar.ChangeRace(_speciesAssetRef.Item.Race);

            // setup a local way to await asynchronously for the character build to finish
            var buildOperation = new UniTaskCompletionSource();
            UnityAction<UMAData> onCharacterUpdated = _ => buildOperation.TrySetResult();

            try
            {
                // it is mandatory to rebuild the character when changing the race so dna and other parameters are properly updated
                Avatar.umaData.CharacterUpdated.AddListener(onCharacterUpdated);
                Avatar.BuildCharacter();
                await buildOperation.Task;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UmaGenie)}] an exception occured while trying to build the UMA avatar after setting the species.\n{exception}");
                return false;
            }
            finally
            {
                Avatar.umaData.CharacterUpdated.RemoveListener(onCharacterUpdated);
            }

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

            var componentCreators = new List<IGenieComponentCreator>(componentAssets.Count + _speciesAssetRef.Item.ComponentCreators.Length);
            componentCreators.AddRange(componentAssets);
            componentCreators.AddRange(_speciesAssetRef.Item.ComponentCreators);

            foreach (IGenieComponentCreator componentCreator in componentCreators)
            {
                GenieComponent component = componentCreator?.CreateComponent();
                if (component is not null)
                {
                    Components.Add(component);
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
#endregion

        private static MaterialBaker GetBakerForLod(string lod, IEnumerable<MaterialBakerLod> bakers)
        {
            foreach (MaterialBakerLod bakerLod in bakers)
            {
                if (bakerLod.lod == lod && bakerLod.baker)
                {
                    return bakerLod.baker;
                }
            }

            return null;
        }

        [Serializable]
        public struct MaterialBakerLod
        {
            public string lod;
            public MaterialBaker baker;
        }
    }
}
