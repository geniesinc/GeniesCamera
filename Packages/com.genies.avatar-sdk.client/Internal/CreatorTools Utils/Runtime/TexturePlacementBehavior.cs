using UnityEngine;
using UnityEngine.Rendering.Universal;
using UMA;
using UMA.CharacterSystem;


namespace Genies.Components.CreatorTools.TexturePlacement
{

    public enum ProjectionType : ushort
    {
        PerProjectorPixelRaycast = 0,
        SingleTileUVRemapRenderFeature = 1,
        SingleTileUVsFromTextureRenderFeature = 2,
        SingleTileDirectRemapTexture2Texture = 3
    }

    /// <summary>
    /// Serves as the external UI facing class for the Free Texture Placement feature.
    /// * fetches Unity Multipurpse Avatar data (DynamicCharacterAvatar and UMA data) from
    /// the given AvatarRoot.
    /// * ProceduralRayguide property allows specification of substrate geometry from which
    ///   rays are cast (curved quad, partial cylinder)
    /// * provides entry / exit points for projection mode
    /// </summary>

    public class TexturePlacementBehavior : MonoBehaviour
    {
        // The node in the UMA Avatar hierarchy that contains
        // DynamicCharacterAvatar and UMAData, and is above the
        // one that has the SkinnedMeshRenderer
        public GameObject AvatarRoot;

        // RenderFeature not in use currently, may return for decoupled uv projection data from image
        public UniversalRendererData URPRenderer;
        public Material DrawMaterial;
        public Material DrawMaterialSingleTile;

        public CylinderMeshGenerator ProceduralRayguide;
        public ProjectionType ProjectionType;

        private DynamicCharacterAvatar _dca;
        private UMAData _umaData;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private MeshCollider _meshCollider;
        // # of frames to delay before baking collision mesh (wait for avatar to stop moving)
        private int _collisionMeshBakeDelay;

        // Tattooenator does the actual projection
        private Tattooenator _tattooenator;
        public Tattooenator Tattooenator { get { return _tattooenator; } }


        private void OnDynamicCharacterAvatarUpdated(UMAData umadata)
        {
            // refresh UMA related data
            _umaData = umadata;
            TraverseChildren(AvatarRoot.transform);
            if (_skinnedMeshRenderer is null)
            {
                Debug.LogWarning("didn't find SkinnedMeshRenderer in AvatarRoot traversal");
            }

            //_tattooenator.OnCharacterUpdated();   // not in use atm
        }

        public void Start()
        {
            
        }

        public void BakeCollisionMeshAfterDelay()
        {
            if (_skinnedMeshRenderer != null && _meshCollider.sharedMesh == null)
            {
                if (_collisionMeshBakeDelay == 0)
                {
                    Mesh captureMesh = new Mesh();
                    _skinnedMeshRenderer.BakeMesh(captureMesh);
                    _meshCollider.sharedMesh = captureMesh;
                    _tattooenator.MeshCollider = _meshCollider;
                } else
                {
                    _collisionMeshBakeDelay--;
                }
            }
        }


        /// <summary>
        /// This method looks for the DynamicCharacterAvatar in the node hierarchy
        /// and sets the _dca and _umaData member variables.
        /// </summary
        private void PreTraverseChildren(Transform p)
        {
            // ignore rigging stuff
            if (p.gameObject.name == "Root" ||
                p.gameObject.name == "chaos_rig" ||
                p.gameObject.name == "RigComponents")
            {
                return;
            }

            bool found = false;
            if (p.TryGetComponent<DynamicCharacterAvatar>(out _dca))
            {
                _dca.CharacterUpdated.AddListener(OnDynamicCharacterAvatarUpdated);
                found = true;
            }

            if (p.TryGetComponent<UMAData>(out _umaData))
            {
                found = true && found;
            }

            if (found)
            {
                return;
            }

            for (int i = 0; i < p.childCount; i++)
            {
                PreTraverseChildren(p.GetChild(i));
            }
        }

        /// <summary>
        /// This method should be called everytime the UmaGenie updates (OnCharacterUpdated).
        /// It looks for the generated SkinnedMeshRenderer, adds mesh collider, and
        /// assigns a mesh object to the collider.
        /// </summary
        private void TraverseChildren(Transform p)
        {
            // ProxyGeo will have a SMR, but we don't want that one. Root is just bones.
            if (p.gameObject.name == "Root" || p.gameObject.name == "ProxyGeo_UMARenderer")
            {
                return;
            }

            if (_skinnedMeshRenderer != null)
            {
                return;
            }

            if (p.TryGetComponent<SkinnedMeshRenderer>(out SkinnedMeshRenderer skinnedMeshRenderer))
            {
                // found the node in the hierarchy with SkinnedMeshRenderer...
                _skinnedMeshRenderer = skinnedMeshRenderer;

                if (!AvatarRoot.GetComponent<MeshCollider>())
                {
                    // add MeshCollider
                    _meshCollider = p.gameObject.AddComponent<MeshCollider>() as MeshCollider;
                }

                if (_tattooenator is null)
                {
                    Debug.LogError("Tattooenater component has not been added yet");
                }
                else
                {
                    _tattooenator.SkinnedMeshRenderer = _skinnedMeshRenderer;
                }
                p.gameObject.layer = LayerMask.NameToLayer("Avatar");
            }

            for (int i = 0; i < p.childCount; i++)
            {
                TraverseChildren(p.GetChild(i));
            }
        }

        /// <summary>
        /// Sets up Free Texture Placement Scaffolding
        /// </summary
        public void EnterFreeTexturePlacementMode(GameObject avatarRoot, int collisionMeshBakeDelay = 1)
        {
            AvatarRoot = avatarRoot;
            _collisionMeshBakeDelay = collisionMeshBakeDelay;

            if (_dca == null && _umaData == null)
            {
                PreTraverseChildren(AvatarRoot.transform);
            }

            if (_dca != null && _umaData != null && !AvatarRoot.GetComponent<Tattooenator>())
            {
                // add Tattooenator (which takes care of RenderFeature hooks)
                _tattooenator = AvatarRoot.AddComponent<Tattooenator>() as Tattooenator;
                // set DCA and UMAData
                _tattooenator.DynamicCharacterAvatar = _dca;
                _tattooenator.UmaData = _umaData;
            }
            else
            {
                Debug.LogError("AvatarRoot GO does not have DynamicCharacterAvatar or UMAData component - select the GO that does!");
                return;
            }

#if NOTUSEDCURRENTLY
            if (URPRenderer == null)
            {
                Debug.LogWarning("URPRenderer property is null and needs to be set!");
            }
            else
            {
                // set renderer that contains render feature
                _tattooenator.URPRenderer = URPRenderer;
            }

            if (DrawMaterial == null)
            {
                Debug.LogError("DrawMaterial property used by the RenderFeature is null and needs to be set!");
            }
            else
            {
                _tattooenator.DrawMaterial = DrawMaterial;
                _tattooenator.DrawMaterialSingleTile = DrawMaterialSingleTile;
            }

            // sets DrawMaterial in RenderFeature and subscribes to RenderPass result
            _tattooenator.InitializeRenderFeature();
#endif

            if (ProceduralRayguide != null)
            {
                _tattooenator.ProceduralRayguide = ProceduralRayguide;
            }

            // Assuming that the UmaAvatar has been built once, can call this here
            TraverseChildren(AvatarRoot.transform);
        }

        /// <summary>
        /// Tears down Free Texture Placement Scaffolding
        /// </summary>
        public void ExitFreeTexturePlacementMode()
        {
            Destroy(_meshCollider); _meshCollider = null;
            Destroy(_tattooenator); _tattooenator = null;
            _skinnedMeshRenderer = null;
        }


        public void Update()
        {
            BakeCollisionMeshAfterDelay();
        }
    }
}
