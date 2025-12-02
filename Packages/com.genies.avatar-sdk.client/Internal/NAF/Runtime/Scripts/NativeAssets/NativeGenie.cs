using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using Genies.Utilities;
using GnWrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using UnityEngine;

using Texture = UnityEngine.Texture;

namespace Genies.Naf
{
    /**
     * Loads and manages a Genies Avatar in the Unity scene. You can either set the genie directly from an entity or
     * manually update it with the more granular set methods.
     */
    [RequireComponent(typeof(Animator))]
    public sealed class NativeGenie : MonoBehaviour, IGenie
    {
        // IGenie
        public string                             Species      => GenieSpecies.Unified;
        public string                             SubSpecies   { get; } // TODO ?
        public string                             Lod          { get; set; } = "lod1";
        public GameObject                         Root         => gameObject;
        public GameObject                         ModelRoot    { get; private set; }
        public Transform                          SkeletonRoot => MeshBuilder.SkeletonRoot;
        public Animator                           Animator     => _animator;
        public IReadOnlyList<SkinnedMeshRenderer> Renderers    => MeshBuilder.Renderers;
        public GenieComponentManager              Components   { get; private set; }
        public bool                               IsDisposed   { get; private set; }

        public event Action RootRebuilt;
        public event Action Rebuilt;
        public event Action BlendShapesChanged; // TODO add this to the IGenie interface
        public event Action Disposed;

        // NativeGenie
        public NativeMultiMeshBuilder MeshBuilder { get; private set; }

        // inspector
        [Tooltip("If left empty, a default renderer will be created instead. Use this if you need to set some non-default settings to the native renderer. Also keep in mind that the skeleton root will be overriden by the NativeGenie")]
        [SerializeField] private NativeMultiMeshBuilder nativeRendererPrefab;
        [SerializeField] private MegaSkinTattooSettings tattooSettings;
        [Tooltip("If true, it will try to generate a grounded avatar asset for the animator")]
        public bool groundHumanAvatar = true;
        [Tooltip("If grounding is enabled, grounding won't happen unless the required offset for grounding is greater or equal than this threshold (in meters)")]
        public float groundingThreshold = 0.001f;

        // baking config
        [Header("Bakers")][Space(8)]
        [SerializeField] private MaterialBaker megaSimpleBaker;
        [SerializeField] private MaterialBaker urpBaker;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Tooltip("If true, it will log all NativeGenie rebuild events to the console. It must be set before Awake")]
        public bool logGenieEvents = false;
#endif

        // private state
        private Animator                  _animator;
        private Avatar                    _humanAvatar;
        private uint?                     _humanDescriptionExtraItemHash; // if the current human description comes from an extra, this will be set to the item hash
        private List<GenieComponentExtra> _componentExtras = new();
        private TattoosTexture            _tattoosTexture;
        private List<GenieJointModifier>  _skeletonOffsetModifiers = new();
        private GenieBaker                _genieBaker;
        private SkinnedNativeMeshRenderer _rendererPrefab;

        // helpers used by SetExtras() to avoid adding extras that are already added
        private bool          _addedHumanDescriptionExtra;
        private HashSet<uint> _componentExtrasHashes = new();

        // events state
        private bool    _isEditing;
        private bool    _wasRebuilt;
        private bool    _wasRootRebuilt;
        private bool    _didHumanSkeletonChange;
        private bool    _humanBoundsDirtied;
        private Vector3 _hipsOffset;

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

            _animator = GetComponent<Animator>();
            if (!_animator)
            {
                throw new Exception($"[{nameof(NativeGenie)}] requires an {nameof(Animator)} component");
            }

            // create our standard Genie skeleton root
            Transform skeletonRoot = new GameObject("Root").transform;
            skeletonRoot.SetParent(transform, worldPositionStays: false);

            // create and initialize the Model GameObject that will contain the renderer (use the prefab if provided)
            if (nativeRendererPrefab)
            {
                MeshBuilder = Instantiate(nativeRendererPrefab);
                MeshBuilder.SetSkeletonRoot(skeletonRoot); // override the skeleton root

                ModelRoot = MeshBuilder.gameObject;
                ModelRoot.name = "Model";
                ModelRoot.transform.SetParent(transform, worldPositionStays: false);
            }
            else
            {
                // create a renderer prefab first
                _rendererPrefab = new GameObject("SkinnedNativeMeshRenderer-Prefab").AddComponent<SkinnedNativeMeshRenderer>();
                _rendererPrefab.gameObject.hideFlags = HideFlags.HideAndDontSave;
                _rendererPrefab.SetRenderer(_rendererPrefab.gameObject.AddComponent<SkinnedMeshRenderer>(), skeletonRoot);

                ModelRoot   = new GameObject("Model");
                MeshBuilder = ModelRoot.AddComponent<NativeMultiMeshBuilder>();

                MeshBuilder.rendererPrefab = _rendererPrefab;
                MeshBuilder.SetSkeletonRoot(skeletonRoot);

                // setting the parent after adding the native renderer is important to ensure that Awake() is invoked in the case that the current GameObject is inactive
                ModelRoot.transform.SetParent(transform, worldPositionStays: false);
            }

            // subscribe to NativeRenderer events
            MeshBuilder.UpdatedMesh          += NotifyRebuild;
            MeshBuilder.UpdatedMaterials     += NotifyRebuild;
            MeshBuilder.SkeletonChanged      += NotifyRootRebuild;
            MeshBuilder.HumanSkeletonChanged += HandleHumanSkeletonChanged;
            MeshBuilder.BlendShapesChanged   += BlendShapesChanged;
            MeshBuilder.HumanBoundsDirtied   += HandleHumanBoundsDirtied;

            // TODO this is to support the legacy BlendShapeAnimator behaviour, which only needs to rebuild when the blend shapes change. Remove this once we update it
            MeshBuilder.BlendShapesChanged += NotifyRootRebuild;

            // initialize Components, tattoos texture and baker
            Components      = new GenieComponentManager(this);
            _tattoosTexture = new TattoosTexture(tattooSettings);
            _genieBaker     = new GenieBaker(Lod, megaSimpleBaker, urpBaker);

            // create the Genie reference component
            GenieReference.Create(this, gameObject);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (logGenieEvents)
            {
                Rebuilt += () => Debug.Log($"<color=yellow>NativeGenie was rebuilt: {gameObject.name}</color>");
                RootRebuilt += () => Debug.Log($"<color=yellow>NativeGenie root was rebuilt: {gameObject.name}</color>");
                BlendShapesChanged += () => Debug.Log($"<color=yellow>NativeGenie blend shapes were changed: {gameObject.name}</color>");
            }
#endif
        }

        private void OnDestroy()
        {
            Dispose();
        }

#region Set methods
        public void SetGenie(Entity entity)
        {
            bool edit = !_isEditing;
            if (edit)
            {
                BeginEditing();
            }

            MeshBuilder.RebuildMesh(entity, forced: true);

            using var skeletonOffset = SkeletonOffset.GetFrom(entity);
            SetSkeletonOffset(skeletonOffset);

            using var tattoos = MegaSkinTattoos.GetFrom(entity);
            SetTattoos(tattoos);

            using EntityExtras extras = EntityExtras.GetFrom(entity);
            SetExtras(extras);

            if (edit)
            {
                EndEditing();
            }
        }

        public void SetSkeletonOffset(SkeletonOffset skeletonOffset)
        {
            // clear the skeleton modifier
            bool cleared = false;
            if (Components.TryGet(out SkeletonModifier skeletonModifier))
            {
                skeletonModifier.RemoveModifiers(_skeletonOffsetModifiers);
                _skeletonOffsetModifiers.Clear();
                cleared = true;
            }

            if (skeletonOffset.IsNull())
            {
                if (cleared)
                {
                    NotifyRebuild();
                }

                return;
            }

            if (skeletonModifier is null)
            {
                skeletonModifier = new SkeletonModifier();
                if (!Components.Add(skeletonModifier))
                {
                    Debug.LogError($"Couldn't set a {nameof(SkeletonModifier)} genie component. Native skeleton offset will be ignored...");
                    return;
                }
            }

            using VectorString boneNames = skeletonOffset.GetOffsetBoneNames();
            foreach (string boneName in boneNames)
            {
                using BoneOffset offset = skeletonOffset.GetOffset(boneName);

                var modifier = new GenieJointModifier(boneName);
                modifier.PositionOperation = modifier.RotationOperation = modifier.ScaleOperation = JointModifier.Operation.Offset;
                modifier.Position = Marshal.PtrToStructure<Vector3>(offset.Position());
                modifier.Rotation = Marshal.PtrToStructure<Vector3>(offset.EulerRotation());
                modifier.Scale    = Marshal.PtrToStructure<Vector3>(offset.Scale());

                skeletonModifier.AddModifier(modifier);
                _skeletonOffsetModifiers.Add(modifier);
            }

            NotifyRebuild();
        }

        /**
         * Sets the current HumanDescription for the genie.
         */
        public void SetHumanDescription(HumanDescription humanDescription)
        {
            /**
             * The SkeletonModifier component that we use for skeleton offsets requires this to be true. If set to false
             * then each time the genie is rebuilt the component will build a new Avatar asset with the option enabled.
             */
            humanDescription.hasTranslationDoF = true;

            var skeleton = new SkeletonBone[humanDescription.skeleton.Length + 1];

            // add the Genie GameObject as the first bone (this is required by the animator)
            skeleton[0] = new SkeletonBone
            {
                name     = Root.name,
                position = Vector3.zero,
                rotation = Quaternion.identity,
                scale    = Vector3.one
            };

            // copy the rest of the bones
            for (int i = 1; i < skeleton.Length; ++i)
            {
                skeleton[i] = humanDescription.skeleton[i - 1];
            }

            // update the human description with the updated skeleton and set it to the native renderer
            humanDescription.skeleton = skeleton;
            MeshBuilder.SetHumanDescription(humanDescription);
            _humanDescriptionExtraItemHash = null;
        }

        public void SetTattoos(MegaSkinTattoos tattoos)
        {
            _tattoosTexture.ClearAllTattoos();

            if (tattoos.IsNull())
            {
                NotifyRebuild(); // we just cleared the tattoos
                return;
            }

            for (int slotIndex = 0; slotIndex < GnCoreWrapper.MegaSkinTattooSlotCount; ++slotIndex)
            {
                using GnWrappers.Texture texture = tattoos.GetTattoo((MegaSkinTattooSlot)slotIndex);
                if (texture.IsNull())
                {
                    continue;
                }

                using Ref<Texture> textureRef = texture.AsUnityTexture();
                if (textureRef.IsAlive)
                {
                    _tattoosTexture.SetTattoo(slotIndex, textureRef.Item);
                }
            }

            foreach (SkinnedNativeMeshRenderer nativeRenderer in MeshBuilder.NativeRenderers)
            {
                IReadOnlyList<NativeMaterial> materials = nativeRenderer.Materials;
                foreach (NativeMaterial nativeMaterial in materials)
                {
                    if (nativeMaterial.Material.shader.name.Contains("MegaSkin"))
                    {
                        _tattoosTexture.ApplyTo(nativeMaterial.Material);
                    }
                }
            }

            NotifyRebuild();
        }

        public void SetExtras(EntityExtras extras)
        {
            bool currentHdCameFromExtras = DidCurrentHumanDescriptionCameFromExtras();

            // remove any previous component extras that are not part of the given extras
            RemoveOldComponentExtras(extras);

            // reset helpers to track what extras are going to be added
            _addedHumanDescriptionExtra = false;
            _componentExtrasHashes.Clear();

            // add extras (this will populate the previous helpers)
            AddExtras(extras);

            // remove the previous human description if it was an extra and no human description extra was added
            if (!_addedHumanDescriptionExtra && currentHdCameFromExtras)
            {
                ClearHumanDescription();
            }

            // remove any previous component extras that were not part of the given extras
            for (int i = 0; i < _componentExtras.Count; ++i)
            {
                GenieComponentExtra componentExtra = _componentExtras[i];
                if (_componentExtrasHashes.Contains(componentExtra.ItemHash))
                {
                    continue;
                }

                Components.Remove(componentExtra.Component);
                componentExtra.ComponentCreator.Dispose();
                _componentExtras.RemoveAt(i--);
            }
        }

        public void AddExtras(EntityExtras extras)
        {
            if (extras is null || extras.IsNull())
            {
                return;
            }

            /**
             * Do a first pass, only for the human description extra. This is important because any component extra that
             * may depend on the human description will find it already set (i.e.: Dynamics).
             */
            uint size = extras.Size();
            for (uint i = 0; i < size; ++i)
            {
                using EntityExtrasItem item = extras.Get(i);
                if (item.Type() == "unity/human-description")
                {
                    AddHumanDescriptionExtra(item);
                }
            }

            // second pass for the rest of extras
            for (uint i = 0; i < size; ++i)
            {
                using EntityExtrasItem item = extras.Get(i);
                if (item.Type() != "unity/human-description")
                {
                    AddExtra(item);
                }
            }
        }

        /**
         * If the given extras item is of a recognized type for the NativeGenie, it will get added.
         */
        public void AddExtra(EntityExtrasItem item)
        {
            switch (item.Type())
            {
                case "unity/human-description":       AddHumanDescriptionExtra(item); break;
                case "unity/genie-component-creator": AddComponentExtra(item); break;
            }
        }
#endregion

#region Clear methods
        public void ClearGenie()
        {
            bool edit = !_isEditing;
            if (edit)
            {
                BeginEditing();
            }

            ClearHumanAvatar(); // clear this first to avoid some annoying warnings from the animator because its not finding the bones
            ClearSkeletonOffset();
            ClearTattoos();
            ClearExtras();
            MeshBuilder.Clear();

            if (edit)
            {
                EndEditing();
            }
        }

        public void ClearSkeletonOffset()
        {
            if (!Components.TryGet(out SkeletonModifier skeletonModifier))
            {
                return;
            }

            skeletonModifier.RemoveModifiers(_skeletonOffsetModifiers);
            _skeletonOffsetModifiers.Clear();

            NotifyRebuild();
        }

        public void ClearHumanDescription()
        {
            MeshBuilder.ClearHumanDescription();
            _humanDescriptionExtraItemHash = null;
            ClearHumanAvatar();
        }

        public void ClearTattoos()
        {
            _tattoosTexture.ClearAllTattoos();
            NotifyRebuild();
        }

        public void ClearExtras()
        {
            if (DidCurrentHumanDescriptionCameFromExtras())
            {
                ClearHumanDescription();
            }

            foreach (GenieComponentExtra componentExtra in _componentExtras)
            {
                Components.Remove(componentExtra.Component);
                componentExtra.ComponentCreator.Dispose();
            }

            _componentExtras.Clear();
        }
#endregion

        public bool DidCurrentHumanDescriptionCameFromExtras()
        {
            return _humanDescriptionExtraItemHash.HasValue;
        }

        public bool DidComponentCameFromExtras(GenieComponent component)
        {
            foreach (GenieComponentExtra componentExtra in _componentExtras)
            {
                if (componentExtra.Component == component)
                {
                    return true;
                }
            }

            return false;
        }

        /**
         * Use this method if you are going to modify the NativeGenie with any of the set methods, or modifying its
         * native renderer. Call EndEditing() at the end to trigger the rebuild events.
         */
        public void BeginEditing()
        {
            if (_isEditing)
            {
                EndEditing();
            }

            _isEditing              = true;
            _wasRebuilt             = false;
            _wasRootRebuilt         = false;
            _didHumanSkeletonChange = false;
            _humanBoundsDirtied     = false;
        }

        public void NotifyRebuild()
        {
            if (_isEditing)
            {
                _wasRebuilt = true;
            }
            else
            {
                Rebuilt?.Invoke();
            }
        }

        public void NotifyRootRebuild()
        {
            if (_isEditing)
            {
                _wasRootRebuilt = true;
            }
            else
            {
                RootRebuilt?.Invoke();
            }
        }

        public void EndEditing()
        {
            if (!_isEditing)
            {
                return;
            }

            if (_didHumanSkeletonChange)
            {
                RebuildHumanAvatar();
            }
            else if (_humanBoundsDirtied)
            {
                CheckHipsOffset();
            }

            if (_wasRebuilt)
            {
                Rebuilt?.Invoke();
            }

            if (_wasRootRebuilt)
            {
                RootRebuilt?.Invoke();
            }

            _isEditing              = false;
            _wasRebuilt             = false;
            _wasRootRebuilt         = false;
            _didHumanSkeletonChange = false;
            _humanBoundsDirtied     = false;
        }

#region IGenie
        public async UniTask<IGenie> CloneAsync(int onLayer = -1)
        {
            return await CloneGenie.CreateAsync(this, onLayer);
        }

        public async UniTask<IGenie> BakeAsync(Transform parent = null, bool urpBake = false)
        {
            return await _genieBaker.BakeAsync(this, parent, urpBake);
        }

        public UniTask<IGenieSnapshot> TakeSnapshotAsync(Transform parent = null, bool urpBake = false)
        {
            return _genieBaker.TakeSnapshotAsync(this, parent, urpBake);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            Rebuilt = null;
            RootRebuilt = null;

            // unsubscribe to NativeRenderer events
            MeshBuilder.UpdatedMesh          -= NotifyRebuild;
            MeshBuilder.UpdatedMaterials     -= NotifyRebuild;
            MeshBuilder.SkeletonChanged      -= NotifyRootRebuild;
            MeshBuilder.HumanSkeletonChanged -= HandleHumanSkeletonChanged;
            MeshBuilder.BlendShapesChanged   -= BlendShapesChanged;
            MeshBuilder.HumanBoundsDirtied   -= HandleHumanBoundsDirtied;

            // TODO this is to support the legacy BlendShapeAnimator behaviour, which only needs to rebuild when the blend shapes change. Remove this once we update it
            MeshBuilder.BlendShapesChanged -= NotifyRootRebuild;

            // remove all components
            Components.RemoveAll();

            // destroy GameObjects created on awake (this will also destroy NativeRenderer and skeleton)
            if (SkeletonRoot)
            {
                Destroy(SkeletonRoot.gameObject);
            }

            if (ModelRoot)
            {
                Destroy(ModelRoot);
            }

            if (_rendererPrefab)
            {
                Destroy(_rendererPrefab);
            }

            // dispose tattoo texture
            _tattoosTexture.Dispose();

            // destroy animator
            if (_humanAvatar)
            {
                Destroy(_humanAvatar);
            }

            // dispose extras
            foreach (GenieComponentExtra componentExtra in _componentExtras)
            {
                componentExtra.ComponentCreator.Dispose();
            }

            _componentExtras.Clear();

            /**
             * Since the NativeGenie is a component, we destroy it automatically on dispose. The GameObject should never
             * be destroyed here.
             */
            if (this)
            {
                Destroy(this);
            }

            Disposed?.Invoke();
        }
#endregion

        private void HandleHumanSkeletonChanged()
        {
            // rebuild the Animator every time the HumanDescription or the human bones within the skeleton change
            if (_isEditing)
            {
                _didHumanSkeletonChange = true;
            }
            else
            {
                RebuildHumanAvatar();
            }
        }

        private void HandleHumanBoundsDirtied()
        {
            if (_isEditing)
            {
                _humanBoundsDirtied = true;
            }
            else
            {
                CheckHipsOffset();
            }
        }

        private Vector3 GetGroundingHipsOffset()
        {
            if (!MeshBuilder.AreHumanBoundsValid)
            {
                MeshBuilder.PerformHumanBoundsCalculation();
            }

            return MeshBuilder.GroundingHipsOffset ?? default;
        }

        private void CheckHipsOffset()
        {
            if (!MeshBuilder.HumanDescription.HasValue)
            {
                return;
            }

            Vector3 hipsOffset = groundHumanAvatar ? GetGroundingHipsOffset() : Vector3.zero;

            // if the required offset for grounding is greater than the threshold, then we need to rebuild the human avatar
            if (Vector3.Distance(hipsOffset, _hipsOffset) >= groundingThreshold)
            {
                RebuildHumanAvatar();
            }
        }

        private void RebuildHumanAvatar()
        {
            if (!MeshBuilder.HumanDescription.HasValue)
            {
                return;
            }

            // update the hips offset if groundHumanAvatar is true
            _hipsOffset = groundHumanAvatar ? GetGroundingHipsOffset() : Vector3.zero;

            // clear the avatar asset and rebuild it
            ClearHumanAvatar();
            _humanAvatar = MeshBuilder.BuildHumanAvatar(gameObject, _hipsOffset);
            _animator.avatar = _humanAvatar;
            _animator.Update(0.0f);
        }

        private void ClearHumanAvatar()
        {
            _animator.avatar = null;
            if (_humanAvatar)
            {
                Destroy(_humanAvatar);
            }

            _humanAvatar = null;
        }

        // assumes that the given item's type is a human description
        private void AddHumanDescriptionExtra(EntityExtrasItem item)
        {
            uint itemHash = item.DataHash();
            _addedHumanDescriptionExtra = true;

            // if a human description extra was already set, and it is the same (same hash), skip
            if (MeshBuilder.HumanDescription.HasValue && _humanDescriptionExtraItemHash == itemHash)
            {
                return;
            }

            // try to deserialize the human description
            using DynamicAccessor dataAccessor = item.DataAccessor();
            HumanDescription? humanDescription = DeserializeHumanDescription(dataAccessor);

            if (!humanDescription.HasValue)
            {
                return;
            }

            // set the human description if successfully deserialized
            SetHumanDescription(humanDescription.Value);
            _humanDescriptionExtraItemHash = itemHash;
        }

        // hashes of component extras that failed to deserialize (so we don't try again)
        private static readonly HashSet<uint> FailedComponentExtrasHashes = new();

        // assumes that the given item's type is a genie component
        private void AddComponentExtra(EntityExtrasItem item)
        {
            uint itemHash = item.DataHash();

            // if the component previously failed to be deserialized, then it doesn't make sense to try again
            if (FailedComponentExtrasHashes.Contains(itemHash))
            {
                return;
            }

            // check the item hash to avoid deserializing and adding an extra that may be already added
            if (IsComponentExtraAlreadyAdded(itemHash))
            {
                _componentExtrasHashes.Add(itemHash);
                return;
            }

            // try to deserialize the item's data as a ScriptableObject (bson)
            UnityObjectRef<ScriptableObject> soRef;

            try
            {
                using DynamicAccessor dataAccessor = item.DataAccessor();
                soRef = ScriptableObjectConverter.DeserializeFromBson(dataAccessor.Data(), (int)dataAccessor.Size());
            }
            catch (Exception exception)
            {
                FailedComponentExtrasHashes.Add(itemHash);
                Debug.LogError($"[{nameof(NativeGenie)}] failed to deserialize component ScriptableObject: {exception.Message}");
                return;
            }

            // check if the successfully deserialized scriptable object implements IGenieComponentCreator
            if (soRef.Object is not IGenieComponentCreator componentCreator)
            {
                soRef.Dispose();
                FailedComponentExtrasHashes.Add(itemHash);
                Debug.LogError($"[{nameof(NativeGenie)}] deserialized ScriptableObject is not a {nameof(IGenieComponentCreator)}");
                return;
            }

            // create a component and try to add it to the genie components
            GenieComponent component = componentCreator.CreateComponent();
            if (!Components.Add(component))
            {
                soRef.Dispose();
                return;
            }

            // if successfully added, register the component extra into the internal list
            var componentExtra = new GenieComponentExtra
            {
                Component        = component,
                ComponentCreator = soRef,
                ItemHash         = itemHash
            };

            _componentExtras.Add(componentExtra);
            _componentExtrasHashes.Add(itemHash);
        }

        private bool IsComponentExtraAlreadyAdded(uint itemHash)
        {
            foreach (GenieComponentExtra component in _componentExtras)
            {
                if (component.ItemHash == itemHash)
                {
                    return true;
                }
            }

            return false;
        }

        // removes any currently added component extras that are not part of the given new extras
        private void RemoveOldComponentExtras(EntityExtras newExtras)
        {
            if (newExtras is null || newExtras.IsNull())
            {
                return;
            }

            // gather hashes
            HashSet<uint> newExtrasHashes = new();
            uint size = newExtras.Size();
            for (uint i = 0; i < size; ++i)
            {
                using EntityExtrasItem item = newExtras.Get(i);
                if (item.Type() == "unity/genie-component-creator")
                {
                    newExtrasHashes.Add(item.DataHash());
                }
            }

            // remove old extras
            for (int i = 0; i < _componentExtras.Count; ++i)
            {
                GenieComponentExtra componentExtra = _componentExtras[i];
                if (newExtrasHashes.Contains(componentExtra.ItemHash))
                {
                    continue;
                }

                Components.Remove(componentExtra.Component);
                componentExtra.ComponentCreator.Dispose();
                _componentExtras.RemoveAt(i--);
            }
        }

        /**
         * Provided that the accessor contains a <see cref="SerializableHumanDescription"/> serialized as BSON, it tries
         * to deserialize it and return it as <see cref="HumanDescription"/>. Returns null if it fails.
         */
        public static unsafe HumanDescription? DeserializeHumanDescription(DynamicAccessor accessor)
        {
            try
            {
                using var stream = new UnmanagedMemoryStream((byte*)accessor.Data(), (int)accessor.Size());
                using var reader = new BsonDataReader(stream);
                var serializableHd = JsonSerializer.CreateDefault().Deserialize<SerializableHumanDescription>(reader);
                return SerializableHumanDescription.Convert(serializableHd);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(NativeGenie)}] failed to deserialize HumanDescription: {exception.Message}");
                return null;
            }
        }

        /**
         * A GenieComponent that came from EntityExtras.
         */
        private struct GenieComponentExtra
        {
            public GenieComponent                   Component;
            public UnityObjectRef<ScriptableObject> ComponentCreator;
            public uint                             ItemHash;
        }

        [ContextMenu("Clear Genie")]
        private void MenuClearGenie() => ClearGenie();
        [ContextMenu("Apply Default Skeleton Pose")]
        private void MenuApplyDefaultSkeletonPose() => MeshBuilder.ApplyDefaultSkeletonPose();
        [ContextMenu("Apply Human Skeleton Pose")]
        private void MenuApplyHumanSkeletonPose() => MeshBuilder.ApplyHumanSkeletonPose(_hipsOffset);
    }
}
