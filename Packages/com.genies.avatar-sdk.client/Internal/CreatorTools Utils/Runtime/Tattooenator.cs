using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering.Universal;
using UMA;
using UMA.CharacterSystem;
using System.IO;
using Genies.CreatorTools.utils.RenderFeature;

namespace Genies.Components.CreatorTools.TexturePlacement
{
    /// <summary>
    /// Tattooenator prepares input and output textures and launches the raycast and results
    /// processing operation.  Assumes MeshCollider has been provided. The individual rays are
    /// calculated by the ProceduralRayguide as follows: the input texture is mapped to its surface
    /// and each ray goes from the mapped pixel center towards the implicit cylinder center (in
    /// the direction of the target mesh).  For rays that successfully hit the target mesh
    /// (and have nonzero alpha), a uv coordinate is sampled and with this information we can add
    /// the color contribution to the corresponding texel in the output (projected texture).
    /// </summary>
    public class Tattooenator : MonoBehaviour
    {
        // TexturePlacementBehavior will set all these
        [HideInInspector]
        public DynamicCharacterAvatar DynamicCharacterAvatar;
        [HideInInspector]
        public UMAData UmaData;
        [HideInInspector]
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        [HideInInspector]
        public MeshCollider MeshCollider;
        [HideInInspector]
        public CylinderMeshGenerator ProceduralRayguide;
        [HideInInspector]
        public ProjectionType ProjectionType;
        public string TargetMaterialName { get; set; }

        // For RenderFeature
        [HideInInspector]
        public UniversalRendererData URPRenderer;  // Renderer that has the UVProject RenderPass Feature
        [HideInInspector]
        public Material DrawMaterial; // UVProjectorMaterial - texture parameter
        [HideInInspector]
        public Material DrawMaterialSingleTile; // UVProjectorMaterial - vector pair parameter

        // Tattooenator provides these
        public event Action<Texture2D> ProjectedTextureReady;
        public static readonly int ProjectedTextureAlbedoPropertyId = Shader.PropertyToID("_DecalAlbedoTransparency");

        private Texture2D _projectorTexture; private Texture2D _outputTexture;
        private byte[] _data;
        private NativeArray<Color32> _decalPixels;
        private NativeArray<Ray> _rays;
        private int _avatarLayerMask;
        private bool _resultsReady = false;

        // this setting assumes we are rendering in Linear space (not Gamma)
        private const GraphicsFormat _graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;

        private void LookupAvatarAtlasSize()
        {
            var umaGeneratorObj = GameObject.Find("UMA_GLIB");
            if (umaGeneratorObj != null )
            {
                if (umaGeneratorObj.TryGetComponent<UMAGenerator>(out UMAGenerator umaGenerator))
                {
                    if (umaGenerator.atlasResolution > 0 && umaGenerator.atlasResolution <= 2048)
                    {
                        _targetTextureSize.width = umaGenerator.atlasResolution;
                        _targetTextureSize.height = umaGenerator.atlasResolution;
                    }
                }
            }
        }

        // match existing texture size on target, or use default
        // (size doesn't have to match, but image aspect should)
        private (int width, int height) _targetTextureSize = (1024, 1024);
        public void SetTargetTextureSize(int width, int height)
        {
            _targetTextureSize.width = width;
            _targetTextureSize.height = height;
        }

        public void SetProjectorTexture(Texture2D projectorTexture)
        {
            _projectorTexture = projectorTexture;
#if UNITY_EDITOR
            if (_projectorTexture.width > 1024 ||_projectorTexture.height > 1024)
                Debug.Log($"<color=orange>Projector Texture dims: {_projectorTexture.width} x {_projectorTexture.height}</color>");
#endif
            if (_projectorTexture == null)
            {
                Debug.LogError("<color=red>Projector Texture passed to setter is null</color>");
            }

            if (_decalPixels.IsCreated)
            {
                _decalPixels.Dispose();
            }

            _decalPixels = new NativeArray<Color32>(_projectorTexture.GetPixels32(), Allocator.Persistent);
        }

        private UVProjectRenderPassFeature _renderFeature = null;
        public UVProjectRenderPassFeature RenderFeature
        {
            get
            {
                if (_renderFeature == null)
                {
                    if (URPRenderer == null)
                    {
                        Debug.LogError("URPRenderer is null, select the renderer that has the UVProject Render Feature.");
                    }
                    else
                    {
                        _renderFeature = URPRenderer.rendererFeatures.OfType<UVProjectRenderPassFeature>().FirstOrDefault();
                        if (_renderFeature == default(UVProjectRenderPassFeature))
                        {
                            Debug.LogError("UVProject Render Feature not found - add it to the URPRenderer");
                        }
                    }
                }
                return _renderFeature;
            }
        }

        public void OnCharacterUpdated()
        {
            // When using Render Feature, update the DrawMesh of RenderFeature from freshly built _skinnedMeshRenderer,
            // plus set up RenderTexture if it hasn't already been
            //UpdateRenderFeature();  // better to do this on demand just before projection ?
        }

        // Called from Start in TattooPlacementBehavior - at this point
        // we have the URPRenderer and its RenderFeature, plus DrawMaterial (things
        // that are configured at Scene level in gameobject scripts).
        // Because we have the RenderFeature, we can go ahead and subscribe our
        // postRenderPassResult handler even though the RenderPass within the RenderFeature
        // hasn't been created yet (and we can't control that)
        public void InitializeRenderFeature()
        {
            if (RenderFeature == null)
            {
                return;
            }

            if (DrawMaterial)
            {
                RenderFeature.DrawMaterial = DrawMaterial;
            }
            else
            {
                Debug.LogError("DrawMaterial is null, it needs to be set by TexturePlacementBehavior");
            }

            // subscribe to render feature result
            RenderFeature.SubscribeToRenderPassResultTexture(OnTextureResultRendered);
        }

        // This should be called after UMA character rebuilds and the latest SkinnedMeshRenderer
        // has been set in this class
        public void UpdateRenderFeature()
        {
            RenderFeature.DrawMesh = SkinnedMeshRenderer.sharedMesh;
            // one time set up of RenderTexture
            if (UmaData != null && RenderFeature.RenderTex == null)
            {
                RenderFeature.RenderTex = RenderTexture.GetTemporary(_targetTextureSize.width, _targetTextureSize.height, 0, _graphicsFormat);
            }
        }

        private void Saveout(Texture2D tex)
        {
#if UNITY_EDITOR
            var path = $"{Application.streamingAssetsPath}/{tex.name}.png";
            File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
#endif
        }

        // callback for RenderFeature
        public void OnTextureResultRendered(Texture2D tex)
        {
            Debug.Log("<color=magenta>OnTextureResultRendered</color>");
            Saveout(tex);
            RenderFeature.ShouldExecute = false;
        }

        private static byte Float2Byte(float f)
        {
            return (byte)Math.Floor(f >= 1.0 ? 255 : f * 256.0);
        }

        // single thread brute force (not used in production)
        public void DoRayCast(Mesh mesh, UnityEngine.Ray[] rays, int rows, int cols)
        {
            // Get first and last ray samples representing the minimum and maximum uv coords of sample window.
            // This uv span multiplied by the the target texture size will determine the size of the texel block
            // that we will write corresponding projector uv coords into.

            var results = new RaycastHit[rays.Length];

            for (int i = 0; i < rays.Length; i++)
            {
                int vertexIndex = (i == 0) ? 0 : mesh.vertices.Length - 1;
                int normalIndex = (i == 0) ? 0 : mesh.normals.Length - 1;

                Vector3 origin = transform.TransformPoint(mesh.vertices[vertexIndex]);
                Vector3 dir    = transform.TransformDirection(mesh.normals[normalIndex]);

                Physics.Raycast(origin, dir, out results[i], Mathf.Infinity, _avatarLayerMask);
            }

            int last = results.Length - 1;
            // TODO: fix hardcode - get texture dimensions from target material
            int texdim = 1024;
            if (results[0].collider != null && results[last].collider != null)
            {
                Vector2 pxmin = results[0].textureCoord * (float)texdim;
                Vector2 pxmax = results[last].textureCoord * (float)texdim;
                // maybe if there's no seam try to do it in one shot
                Debug.Log($"<color=cyan>Tmin {results[0].textureCoord.x}, {results[0].textureCoord.y}   Tmax {results[last].textureCoord.x}, {results[last].textureCoord.y} </color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>Raycast miss, please reposition the Tattooenator tool</color>");
                return;  // TODO: could try subtiles
            }

            // test
            rows = 1; cols = 1;
            int w = _projectorTexture.width;
            int h = _projectorTexture.height;

            Texture2D sampleTexture = new Texture2D(texdim, texdim, TextureFormat.RGBA32, mipChain: true, linear: true);
            sampleTexture.filterMode = FilterMode.Trilinear;
            var data = new byte[texdim * texdim * 4];
            float puv_span_u = 1.0f / (float)cols;
            float puv_span_v = 1.0f / (float)rows;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    // process tile:
                    // between the pixel @ Tmin*imageRes and the pixel @ Tmax*imageRes,
                    // set the value as the linear interpolation of Pmin->Pmax over T texels
                    int ray_org_row_count = cols + 1;   // it takes N+1 rays to define N tiles
                    RaycastHit bottomleft = results[i * ray_org_row_count + j];    // i, j
                    RaycastHit topright = results[(i + 1) * ray_org_row_count + j + 1];   // i+1, j+1
                    Debug.Log($"<color=yellow>Tile {i},{j}: min {bottomleft.textureCoord.x}, {bottomleft.textureCoord.y}  max {topright.textureCoord.x}, {topright.textureCoord.y}</color>");
                    if (bottomleft.collider == null || topright.collider == null)
                    {
                        continue;   // miss occurred for this tile
                    }
                    Vector2 txmin = new Vector2(0.5846946f, 0.4505511f) * (float)texdim;
                    Vector2 txmax = new Vector3(0.6336557f, 0.4995905f) * (float)texdim;

                    int tx, ty, tx_max, ty_max, tx_span_x, tx_span_y;
                    tx = Mathf.RoundToInt(txmin.x); tx_max = Mathf.RoundToInt(txmax.x);
                    ty = Mathf.RoundToInt(txmin.y); ty_max = Mathf.RoundToInt(txmax.y);
                    tx_span_x = Mathf.RoundToInt(txmax.x - txmin.x);
                    tx_span_y = Mathf.RoundToInt(txmax.y - txmin.y);

                    // amount projector coordinates change per texel in a tile
                    float pincr_x = puv_span_u / (float)tx_span_x;
                    float pincr_y = puv_span_v / (float)tx_span_y;

                    // projector coordinate at the start (bottom right) of the current tile
                    Vector2 Pmin = new Vector2(pincr_x * j, pincr_y * i);

                    for (int s = tx; s < tx_max; s++)
                    {
                        float red = Pmin.x + pincr_x * (s - tx);
                        //red += 0.5f * pincr_x;
                        for (int t = ty; t < ty_max; t++)
                        {
                            // value at texel s,t
                            float green = Pmin.y + pincr_y * (t - ty);
                            //green += 0.5f * pincr_y;
                            Color projTexColor = _projectorTexture.GetPixelBilinear(red, green, 4);
                            data[t * (texdim * 4) + (s * 4)] = Float2Byte(projTexColor.r); //Float2Byte(red);
                            data[t * (texdim * 4) + (s * 4) + 1] = Float2Byte(projTexColor.g); //Float2Byte(green);
                            data[t * (texdim * 4) + (s * 4) + 2] = Float2Byte(projTexColor.b); //0;
                            data[t * (texdim * 4) + (s * 4) + 3] = Float2Byte(projTexColor.a); //255;
                        }
                    }
                }
            }
            sampleTexture.name = "projected_color";
            sampleTexture.SetPixelData(data, 0);   // will allow specifying just level 0 and hardware does the rest

            sampleTexture.Apply();
            AddOverlay(sampleTexture);

            Saveout(sampleTexture);
        }

        // only requires 2 raycasts, but assumes continous uvs (not used in production)
        public void SingleTileTwoCornerRaycast()
        {
            // switch material on render feature
            RenderFeature.DrawMaterial = DrawMaterialSingleTile;
            UpdateRenderFeature();  // DrawMesh and RenderTexture

            // Replace RaycastCommand batch with plain Physics.Raycast using QueryParameters.
            QueryParameters[] queries = ProceduralRayguide.GetSingleTile2CornerRaysQ();

            Mesh mesh = ProceduralRayguide.Mesh;
            var results = new RaycastHit[queries.Length];

            // Loop over the two corners (first and last vertices)
            for (var i = 0; i < queries.Length; i++)
            {
                var vertexIndex = (i == 0) ? 0 : mesh.vertices.Length - 1;
                var normalIndex = (i == 0) ? 0 : mesh.normals.Length - 1;

                Vector3 origin = transform.TransformPoint(mesh.vertices[vertexIndex]);
                Vector3 dir    = transform.TransformDirection(mesh.normals[normalIndex]);

                Physics.Raycast(origin, dir, out results[i], Mathf.Infinity, queries[i].layerMask, queries[i].hitTriggers);
            }


            var last = results.Length - 1;
            // raycasts from both corners were successful
            if (results[0].collider != null && results[last].collider != null)
            {
                var projectuv = new Vector4(0f, 0f, 1f, 1f);  // assume full image tile for now
                var targetuv = new Vector4(results[0].textureCoord.x, results[0].textureCoord.y,  // min corner
                                               results[last].textureCoord.x, results[last].textureCoord.y);  // max corner
                Debug.Log("Projector" + projectuv.ToString());
                Debug.Log("Target" + targetuv.ToString());
                DrawMaterialSingleTile.SetVector("_ProjectorUV", projectuv);
                DrawMaterialSingleTile.SetVector("_TargetUV", targetuv);
                RenderFeature.ShouldExecute = true;
            }
            else
            {
                Debug.LogWarning("<color=orange>Raycast miss, please reposition the Tattooenator tool</color>");
            }
        }

        public void Project()
        {
            DoProjection().Forget();
        }

        public async UniTask DoProjection()
        {
            //Debug.Log("<color=magenta>Doing Projection: </color>");
            //UnityEngine.Profiling.Profiler.BeginSample("Tattooenator.DoProjection");
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            await PreRayCast();
            stopwatch.Stop();
            double preRaycastTime = stopwatch.ElapsedMilliseconds;
            Debug.Log($"    <color=magenta> preRaycast elapsed time {preRaycastTime} ms</color>");
            stopwatch.Reset();
            stopwatch.Start();

            switch (ProjectionType)
            {
                case ProjectionType.PerProjectorPixelRaycast:
                    //Debug.Log("    <color=magenta>Per projector pixel raycast</color>");
                    PerPixelRaycastJobified();
                    break;
                case ProjectionType.SingleTileUVRemapRenderFeature:
                    //Debug.Log("    <color=magenta>Single tile UV remap render feature</color>");
                    SingleTileTwoCornerRaycast();
                    break;
                case ProjectionType.SingleTileUVsFromTextureRenderFeature:
                    //Debug.Log("    <color=magenta>Single tile UVs from texture render feature</color>");
                    break;
                case ProjectionType.SingleTileDirectRemapTexture2Texture:
                    //Debug.Log("    <color=magenta>Single tile direct remap: projector texture to target texture</color>");
                    var rays = ProceduralRayguide.GetRaysFromCurrentMeshRay();
                    //var commands = new NativeArray<RaycastCommand>(, Allocator.TempJob);
                    DoRayCast(ProceduralRayguide.Mesh, rays, ProceduralRayguide.Subdivisions, ProceduralRayguide.LengthwiseSubdivisions);
                    //var commands = new NativeArray<RaycastCommand>(GetRaysFromCurrentMesh(64, 64), Allocator.TempJob);
                    //_tattooenatorRef.DoRaycast(commands, 64, 64);
                    break;
            }

            stopwatch.Stop();
            //UnityEngine.Profiling.Profiler.EndSample();
            //Debug.Log($"    <color=magenta> preRaycast elapsed time {preRaycastTime} sec</color>");
            //Debug.Log($"    <color=magenta> projection elapsed time {stopwatch.Elapsed.TotalSeconds} sec</color>");
            //Debug.Log($"    <color=magenta>   -> total elapsed time {preRaycastTime + stopwatch.Elapsed.TotalSeconds} sec</color>");
            return;
        }

        private async UniTask PreRayCast()
        {
            //UnityEngine.Profiling.Profiler.BeginSample("Tattooenator.PreRayCast");
            // assumes collision mesh is set up once avatar has stopped moving in ProjectTexture entrypoint
            CreateOutputTexture("jobTexture", mipmap: true, linearColorSpace: false);
            _rays = await ProceduralRayguide.GetRaysFromImplicitCylinderAsync(_projectorTexture.width, _projectorTexture.height);
            //UnityEngine.Profiling.Profiler.EndSample();
        }


        // Creates and configures an output texture the same size as the target object's texture
        // Texture will be initialized with texture data set to clear color, to access use:
        // byte[] data = outputTexture.GetRawTextureData()
        private void CreateOutputTexture(string name, bool mipmap, bool linearColorSpace)
        {
            if (_outputTexture != null &&
                _outputTexture.width == _targetTextureSize.width && _outputTexture.height == _targetTextureSize.height)
            {
                // if we're re-using the output texture, reset data to clearcolor
                _data = new byte[_targetTextureSize.width * _targetTextureSize.height * 4];
                return;
            }
            int width = _targetTextureSize.width;
            int height = _targetTextureSize.height;

            // create empty output texture to print the projection into
            _outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, mipmap, linearColorSpace);
            _outputTexture.name = name;
            if (mipmap)
            {
                _outputTexture.filterMode = FilterMode.Trilinear;
                // set bias too?
            } else
            {
                _outputTexture.filterMode = FilterMode.Bilinear;
            }

            // set clear color black, byte default = 0  (we could also initialize it with the pixels of the target texture)
            // we'll set the PixelData into the Texture after we're doing filling in our projected texture data
            _data = new byte[width * height * 4];
            //outputTexture.LoadRawTextureData(data);  // expects all mip levels to be included in data if mipChain=true
            // (unless we do some fancy downscale algorithm on cpu to make the mip levels sharper, let gpu auto-generate them)
            //_outputTexture.SetPixelData(data, 0);   // will allow specifying just level 0 and hardware does the rest

            //_data = _outputTexture.GetRawTextureData();  // pull the data out now while we're on the main thread
        }


        private Raycasternator _raycasternator;

        public void PerPixelRaycastJobified()
        {
            _raycasternator = new Raycasternator();
            _raycasternator.OutputTextureDims = (_outputTexture.width, _outputTexture.height);
            _raycasternator.OutputTextureData = _data;
            _raycasternator.Rays = _rays;
            _raycasternator.ProjectorTextureDims = (_projectorTexture.width, _projectorTexture.height);
            _raycasternator.DecalPixels = _decalPixels;
            _raycasternator.LayerMask = _avatarLayerMask;
            _raycasternator.ColliderInstanceID = MeshCollider.GetInstanceID();
            _raycasternator.ColliderUvs = new NativeArray<Vector2>(MeshCollider.sharedMesh.uv, Allocator.Persistent);
            _raycasternator.ColliderTriangles = new NativeArray<int>(MeshCollider.sharedMesh.triangles, Allocator.Persistent);

            _raycasternator.SubscribeToResultNotification(OnJobComplete);
            _raycasternator.LaunchJobChain();
        }

        public void OnJobComplete()
        {
            _resultsReady = true;
        }

        /// <summary>
        /// Called when the raycasting, and subsequent results processing is complete
        /// </summary>
        public void FinalizeOnMainThread()
        {
            _resultsReady = false;
            // Apply the projection to the output texture
            // note that if mipChain option was true, the rest of the mip levels will be
            //  autogenerated on gpu
            _data = _raycasternator.OutputTextureData;
            _outputTexture.SetPixelData(_data, 0);  // shaved off 4 ms over SetPixel inside loop
            _outputTexture.Apply(updateMipmaps: true);   // upload to gpu
            UpdateFreelyPlacedTexture(_outputTexture);   // update live material
            ProjectedTextureReady?.Invoke(_outputTexture);
        }


        public void AddOverlay(Texture2D outputTexture)
        {
            string assetName = "TestOverlay";
            int targetIndex = 0;
            int matIndex = (targetIndex == 0) ? 0 : UmaData.generatedMaterials.materials.Count - 1;
            UMAMaterial targetMaterial = UmaData.generatedMaterials.materials[matIndex].umaMaterial;

            // create overlay data asset
            var overlayDataAsset = ScriptableObject.CreateInstance<OverlayDataAsset>();

            overlayDataAsset.overlayName = assetName;
            overlayDataAsset.material = targetMaterial;
            overlayDataAsset.overlayType = OverlayDataAsset.OverlayType.Normal;
            // overlay must match existing channel count ?
            int texCount = (targetIndex == 0) ? 4 : 3;
            overlayDataAsset.textureList = new Texture[texCount];
            Debug.Log("[Tattooenator] outputTexture.name = " + outputTexture.name);
            overlayDataAsset.textureList[0] = outputTexture;

            // Add to global UMA index
            UMAAssetIndexer.Instance.ProcessNewItem(overlayDataAsset, false, false);

            //create new overlay
            OverlayData targetOverlay = new OverlayData(overlayDataAsset);

            // add to recipe
            UMAData.UMARecipe uRecipe = UmaData.umaRecipe;
            uRecipe.slotDataList[targetIndex].AddOverlay(targetOverlay);
            Debug.Log("[Tattooenator] " + uRecipe.slotDataList[targetIndex]);

            // mark texture dirty on avatar
            UmaData.Dirty(false, true, false);

        }

        /// <summary>
        /// Sets the texture on the appropriate live SkinnedMeshRenderer material
        /// </summary>
        /// <param name="outputTexture"></param>
        public void UpdateFreelyPlacedTexture(Texture2D outputTexture)
        {
            var mats = SkinnedMeshRenderer.sharedMaterials;
            foreach (var mat in mats)
            {
                if (mat.name.Contains(TargetMaterialName))
                {
                    mat.SetTexture(ProjectedTextureAlbedoPropertyId, outputTexture);
                    return;
                }
            }

            // otherwise set on the last material ?
            mats[mats.Length - 1].SetTexture(ProjectedTextureAlbedoPropertyId, outputTexture);
            SkinnedMeshRenderer.sharedMaterials = mats;
        }

        private void OnDestroy()
        {
            // TODO: do this in renderfeature?
            if (_renderFeature != null)
            {
                RenderTexture.ReleaseTemporary(_renderFeature.RenderTex);
            }

            _raycasternator?.Dispose();
            _decalPixels.Dispose();
            CleanupSubscribers();
        }

        private void CleanupSubscribers()
        {
            var subscribers = ProjectedTextureReady?.GetInvocationList();
            if (subscribers != null)
            {
                for (int i = 0; i < subscribers.Length; i++)
                {
                    ProjectedTextureReady -= subscribers[i] as Action<Texture2D>;
                }
            }
        }

        // Start is called before the first frame update
        private void Start()
        {
            // The texture to be projected (freely placed) is stored in the material
            // of the projector object - the ProceduralRayGuide currently does double
            // duty as the projector object and the base from which we form the sampling rays
            //var projectorRenderer = ProceduralRayguide.GetComponent<MeshRenderer>();
            //_projectorTexture = projectorRenderer.material.mainTexture as Texture2D;

            _avatarLayerMask = 1 << LayerMask.NameToLayer("Avatar");

            // set output texture size to match body atlas
            LookupAvatarAtlasSize();
        }

        // Update is called once per frame
        private void Update()
        {
            if (_resultsReady)
            {
                FinalizeOnMainThread();
            }
        }

        private void LateUpdate()
        {
            _raycasternator?.CheckForJobCompletion();
        }
    }
}
