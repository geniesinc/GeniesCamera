using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Generic <see cref="IGenie"/> component implementation that can be configured from inspector or code. It features
    /// and LOD system where LODs can be added/removed dynamically (to support lazy loading). This component is intended
    /// to be initialized only once, so LODs are the only thing that can be dynamically changed once the component is
    /// initialized. All the LODs are assumed to have the same renderer configuration with the same mesh skinning.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class Genie : MonoBehaviour, IGenie
    {
        [SerializeField]
        private Config config;

        public string                             Species      => config.species;
        public string                             SubSpecies   => config.species;
        public string                             Lod          => config.lod;
        public GameObject                         Root         => gameObject;
        public GameObject                         ModelRoot    => _lodManager?.ModelRoot;
        public Transform                          SkeletonRoot => config.skeletonRoot;
        public Animator                           Animator     { get; private set; }
        public IReadOnlyList<SkinnedMeshRenderer> Renderers    => _lodManager?.Renderers;
        public GenieComponentManager              Components   { get; private set; }
        public bool                               IsDisposed   { get; private set; }

        public IReadOnlyList<LodConfig> Lods => _lodManager.Lods;

        public LODFadeMode FadeMode
        {
            get => _lodManager.FadeMode;
            set
            {
                _lodManager.FadeMode = value;
                config.fadeMode = value;
            }
        }

        public bool AnimateCrossFading
        {
            get => _lodManager.AnimateCrossFading;
            set
            {
                _lodManager.AnimateCrossFading = value;
                config.animateCrossFading = value;
            }
        }

        /// <summary>
        /// Invoked right before an LOD is being destroyed. The LOD root is passed as an argument.
        /// </summary>
        public event Action<GameObject> DestroyingLod = delegate { };

        // IGenie events
        public event Action RootRebuilt = delegate { };
        public event Action Rebuilt     = delegate { };
        public event Action Disposed    = delegate { };

        // state
        private bool            _initialized;
        private GenieLodManager _lodManager;
        private GenieBaker      _genieBaker;

        public static Genie Create(GameObject gameObject, Config config, Action onDisposedCallback = null)
        {
            var genie = gameObject.AddComponent<Genie>();
            genie.config = config;

            if (onDisposedCallback is not null)
            {
                genie.Disposed += onDisposedCallback;
            }

            genie.Initialize();
            return genie;
        }

        private void Awake()
        {
            Components = new GenieComponentManager(this);
            GenieReference.Create(this, gameObject, disposeOnDestroy: false);
        }

        private void Start()
        {
            Initialize();
        }

        private void OnValidate()
        {
            if (_lodManager is null)
            {
                return;
            }

            _lodManager.FadeMode = config.fadeMode;
            _lodManager.AnimateCrossFading = config.animateCrossFading;
            _lodManager.AutomaticallyCalculateObjectSize = config.automaticallyRecalculateObjectSize;
            _lodManager.ObjectSize = config.objectSize;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            Animator = GetComponent<Animator>();

            if (!Animator)
            {
                Debug.LogError($"[{nameof(Genie)}] {name}: missing Animator component");
            }

            if (!SkeletonRoot || SkeletonRoot.parent != transform)
            {
                Debug.LogError($"[{nameof(Genie)}] {name}: no skeleton root was configured or it is not a direct child of this game object");
            }

            if (!TryInitializeSharedLodConfig())
            {
                return;
            }

            // initialize the lod manager and add the lods
            _lodManager = new GenieLodManager(transform, config.sharedLodConfig);
            _lodManager.FadeMode = config.fadeMode;
            _lodManager.AnimateCrossFading = config.animateCrossFading;
            _lodManager.AutomaticallyCalculateObjectSize = config.automaticallyRecalculateObjectSize;
            _lodManager.ObjectSize = config.objectSize;
            _lodManager.AddLods(config.lods);

            // update lods from config so lods are properly sorted in the inspector (this is not important, just nice to have)
            config.lods.Clear();
            config.lods.AddRange(_lodManager.Lods);

            // create genie baker
            _genieBaker = new GenieBaker(config.lod, config.megaSimpleBaker, config.urpBaker);

            // add components from the config
            var componentCreators = new List<IGenieComponentCreator>();
            if (config.componentAssets is not null)
            {
                componentCreators.AddRange(config.componentAssets);
            }

            if (config.ComponentCreators is not null)
            {
                componentCreators.AddRange(config.ComponentCreators);
            }

            foreach (IGenieComponentCreator componentCreator in componentCreators)
            {
                GenieComponent component = componentCreator?.CreateComponent();
                if (component is not null)
                {
                    Components.Add(component);
                }
            }
        }

        public void AddLods(IEnumerable<LodConfig> lods)
        {
            _lodManager.AddLods(lods);
            OnLodsUpdated();
        }

        public void AddLod(LodConfig lod)
        {
            _lodManager.AddLod(lod);
            OnLodsUpdated();
        }

        public void DestroyLod(int index)
        {
            if (index < 0 || index >= _lodManager.Lods.Count)
            {
                return;
            }

            DestroyingLod.Invoke(_lodManager.Lods[index].root);
            _lodManager.DestroyLod(index);
            OnLodsUpdated();
        }

        public void DestroyAllLods()
        {
            foreach (LodConfig lod in _lodManager.Lods)
            {
                DestroyingLod.Invoke(lod.root);
            }

            _lodManager.DestroyAllLods();
            OnLodsUpdated();
        }

        public async UniTask<IGenie> CloneAsync(int onLayer = -1)
        {
            IGenie genie = await CloneGenie.CreateAsync(this, onLayer);
            return genie;
        }

        public async UniTask<IGenie> BakeAsync(Transform parent = null, bool urpBake = false)
        {
            if (_lodManager.Lods.Count > 1)
            {
                Debug.LogError($"[{nameof(Genie)}] baking genies with multiple LODs is not supported");
                return null;
            }

            return await _genieBaker.BakeAsync(this, parent, urpBake);
        }

        public UniTask<IGenieSnapshot> TakeSnapshotAsync(Transform parent = null, bool urpBake = false)
        {
            if (_lodManager.Lods.Count > 1)
            {
                Debug.LogError($"[{nameof(Genie)}] baking genies with multiple LODs is not supported");
                return UniTask.FromResult<IGenieSnapshot>(null);
            }

            return _genieBaker.TakeSnapshotAsync(this, parent, urpBake);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            Components?.RemoveAll();
            Components = null;

            if (gameObject)
            {
                Destroy(gameObject);
            }

            Disposed?.Invoke();
        }

        private void OnLodsUpdated()
        {
            config.lods.Clear();
            config.lods.AddRange(_lodManager.Lods);
            RootRebuilt.Invoke();
            Rebuilt.Invoke();
        }

        private bool TryInitializeSharedLodConfig()
        {
            if (!config.autoGenerateSharedLodConfig)
            {
                return true;
            }

            if (config.lods is null)
            {
                Debug.LogError($"[{nameof(Genie)}] {name}: cannot automatically generate the shared LOD config. No lods were provided");
                return false;
            }

            if (config.autoGenerateSharedLodConfigIndex < 0 || config.autoGenerateSharedLodConfigIndex >= config.lods.Count)
            {
                Debug.LogError($"[{nameof(Genie)}] {name}: cannot automatically generate the shared LOD config. The specified {nameof(Config.autoGenerateSharedLodConfigIndex)} is out or range");
                return false;
            }

            LodConfig lod = config.lods[config.autoGenerateSharedLodConfigIndex];
            if (!lod.root)
            {
                Debug.LogError($"[{nameof(Genie)}] {name}: cannot automatically generate the shared LOD config. The LOD used to create it has a null or destroyed root");
                return false;
            }

            config.sharedLodConfig = CreateSharedLodConfig(lod.root);
            return true;
        }

        public static SharedLodConfig CreateSharedLodConfig(GameObject lodRoot)
        {
            return CreateSharedLodConfig(lodRoot.GetComponentsInChildren<SkinnedMeshRenderer>());
        }

        public static SharedLodConfig CreateSharedLodConfig(IEnumerable<SkinnedMeshRenderer> renderers)
        {
            var config = new SharedLodConfig
            {
                rendererConfigs = new List<RendererBonesConfig>(),
            };

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                config.rendererConfigs.Add(new RendererBonesConfig
                {
                    rootBone = renderer.rootBone,
                    bones = renderer.bones,
                });
            }

            return config;
        }

        [Serializable]
        public struct Config
        {
            [Header("Required Setup")][Space(8)]
            public string     species;
            public string     subSpecies;
            public string     lod;
            public Transform  skeletonRoot;

            [Header("LODs")][Space(8)]
            public LODFadeMode     fadeMode;
            public bool            animateCrossFading;
            public bool            automaticallyRecalculateObjectSize;
            public float           objectSize;
            public List<LodConfig> lods;
            public bool            autoGenerateSharedLodConfig;
            [Tooltip("The index of the LOD that will be used to auto-generate the shared lod config")]
            public int             autoGenerateSharedLodConfigIndex;
            [Tooltip("Enable autoGenerateSharedLodConfig so this is automatically generated based on the specified autoGenerateSharedLodConfigIndex")]
            public SharedLodConfig sharedLodConfig;

            [Header("Optional Setup")][Space(8)]
            public MaterialBaker             megaSimpleBaker;
            public MaterialBaker             urpBaker;
            public List<GenieComponentAsset> componentAssets;

            /// <summary>
            /// Can only be specified from code and will get added along with <see cref="componentAssets"/>.
            /// </summary>
            public IEnumerable<IGenieComponentCreator> ComponentCreators;
        }

        [Serializable]
        public struct LodConfig
        {
            public GameObject root;
            public float      screenRelativeTransitionHeight;
            public float      fadeTransitionWidth;
        }

        [Serializable]
        public struct SharedLodConfig
        {
            public List<RendererBonesConfig> rendererConfigs;
        }

        [Serializable]
        public struct RendererBonesConfig
        {
            public Transform   rootBone;
            public Transform[] bones;
        }
    }
}
