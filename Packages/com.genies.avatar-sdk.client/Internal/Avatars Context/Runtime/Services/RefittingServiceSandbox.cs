using System;
using System.Collections.Generic;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Genies.Avatars.Context
{
    [DisallowMultipleComponent]
    public sealed class RefittingServiceSandbox : MonoBehaviour, IRefittingService
    {
        private const string LogTag = "<color=magenta>[" + nameof(RefittingServiceSandbox) + "]</color>";

        public enum RefittingSliderMode
        {
            MainRefitting,
            AltRefitting,
            Compare,
        }

        public static RefittingServiceSandbox Instance { get; private set; }

        public GameObject genieParent;

        [Header("Main Refitting Service")]
        public bool mainServiceVerbose;
        public bool useLegacyService = false;
        [Header("Async Refit")]
        public bool mainAsyncRefit;
        public ReferenceShapesLoaderAsset mainShapesLoader;

        [Space(8), Header("Alt Refitting Service")]
        public bool altServiceVerbose;
        [Header("Async Refit")]
        public bool altAsyncRefit;
        public ReferenceShapesLoaderAsset altShapesLoader;

        [Header("Refitting Slider"), Space(4)]
        public RefittingSliderMode mode = RefittingSliderMode.MainRefitting;
        public bool includeBodyDeform = false;
        [Range(0.0f, 1.0f), Tooltip("When mode is:\n    - Main refitting: 0 is no refitting applied and 1 is full main refitting\n    - Alt refitting: 0 is no refitting applied and 1 is full alt refitting\n    - Compare: 0 is full main refitting and 1 is full alt refitting")]
        public float value = 1.0f;

        private RefittingService _legacyRefittingService;
        private IRefittingService _mainRefittingService;
        private IRefittingService _altRefittingService;

        private void Awake()
        {
            Instance = this;
            ReinitializeServices();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                RefreshRefittingSlider();
            }
        }

        private void OnDestroy()
        {
            if (_mainRefittingService is RsRefittingService refittingService)
            {
                refittingService.Dispose();
            }

            if (_altRefittingService is RsRefittingService altRefittingService)
            {
                altRefittingService.Dispose();
            }
        }

        public void SetLegacyService(RefittingService legacyService)
        {
            _legacyRefittingService = legacyService;
            ReinitializeServices();
        }

        public void Refresh()
        {
            ReinitializeServices();
            LoadAllVectorsAsync().Forget();
        }

        public void RefreshRefittingSlider()
        {
            if (!TryGetGenie(out IGenie genie))
            {
                return;
            }

            var controllerRef = genie.Root.GetComponentInChildren<SpeciesGenieControllerReference>();
            if (!controllerRef || controllerRef.Controller is not UnifiedGenieController controller)
            {
                return;
            }

            string deformKey = controller.BodyVariation.CurrentVariation;
            string shapeName = _mainRefittingService.GetBodyVariationBlendShapeName(deformKey);
            string altShapeName = _altRefittingService.GetBodyVariationBlendShapeName(deformKey);
            float weight = 100.0f * value;

            if (includeBodyDeform && mode is not RefittingSliderMode.Compare)
            {
                SetBlendShapeWeight(genie.Renderers, $"bodyOnly_geo_blendShape.{deformKey}", weight);
                SetBlendShapeWeight(genie.Renderers, $"headOnly_geo_blendShape.{deformKey}", weight);
            }
            else
            {
                SetBlendShapeWeight(genie.Renderers, $"bodyOnly_geo_blendShape.{deformKey}", 100.0f);
                SetBlendShapeWeight(genie.Renderers, $"headOnly_geo_blendShape.{deformKey}", 100.0f);
            }

            switch (mode)
            {
                case RefittingSliderMode.MainRefitting:
                    SetBlendShapeWeight(genie.Renderers, shapeName, weight);
                    SetBlendShapeWeight(genie.Renderers, altShapeName, 0.0f);
                    break;
                case RefittingSliderMode.AltRefitting:
                    SetBlendShapeWeight(genie.Renderers, shapeName, 0.0f);
                    SetBlendShapeWeight(genie.Renderers, altShapeName, weight);
                    break;
                case RefittingSliderMode.Compare:
                    SetBlendShapeWeight(genie.Renderers, shapeName, 100.0f - weight);
                    SetBlendShapeWeight(genie.Renderers, altShapeName, weight);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void EnableGenieAnimation()
        {
            if (!TryGetGenie(out IGenie genie))
            {
                return;
            }

            if (genie is EditableGenie editableGenie)
            {
                editableGenie.EnableAnimation();
            }
            else
            {
                genie.Animator.enabled = true;
            }
        }

        public void DisableGenieAnimation()
        {
            if (!TryGetGenie(out IGenie genie))
            {
                return;
            }

            if (genie is EditableGenie editableGenie)
            {
                editableGenie.DisableAnimation();
            }
            else
            {
                SetAvatarTPose(genie);
            }

            // this is just so the facial expressions are reset
            List<BlendShapeAnimator> animators = genie.Components.GetAll<BlendShapeAnimator>();
            foreach (BlendShapeAnimator animator in animators)
            {
                animator.RebuildMappings();
            }
        }

        public UniTask LoadAllVectorsAsync()
        {
            return UniTask.WhenAll(
                LoadAllVectorsAsync(_mainRefittingService, "main", mainServiceVerbose),
                LoadAllVectorsAsync(_altRefittingService, "alt", altServiceVerbose)
            );

            async UniTask LoadAllVectorsAsync(IRefittingService service, string label, bool verbose)
            {
                Stopwatch stopwatch = verbose ? Stopwatch.StartNew() : null;
                await service.LoadAllVectorsAsync();
                if (!verbose)
                {
                    return;
                }

                stopwatch.Stop();
                Debug.Log($"{LogTag} <color=orange>{label}</color> refitting service loading took <color=cyan>{stopwatch.Elapsed.TotalMilliseconds:0.00} ms</color>");
            }
        }

        public string GetBodyVariationBlendShapeName(string bodyVariation)
        {
            return _mainRefittingService.GetBodyVariationBlendShapeName(bodyVariation);
        }

        public UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation)
        {
            return UniTask.WhenAll(
                AddBodyVariationBlendShapeAsync(_mainRefittingService, "main", mainServiceVerbose),
                AddBodyVariationBlendShapeAsync(_altRefittingService, "alt", altServiceVerbose)
            );

            async UniTask AddBodyVariationBlendShapeAsync(IRefittingService service, string label, bool verbose)
            {
                Stopwatch stopwatch = verbose ? Stopwatch.StartNew() : null;
                int prevBlendShapeCount = asset.GenieType == GenieTypeName.NonUma ? asset.MeshAssets[0].BlendShapes.Length : asset.Slots[0].meshData.blendShapes.Length;
                await service.AddBodyVariationBlendShapeAsync(asset, bodyVariation);
                int currBlendShapeCount = asset.GenieType == GenieTypeName.NonUma ? asset.MeshAssets[0].BlendShapes.Length : asset.Slots[0].meshData.blendShapes.Length;

                // only log for assets that were refitted
                if (!verbose || prevBlendShapeCount == currBlendShapeCount)
                {
                    return;
                }

                stopwatch.Stop();
                Debug.Log($"{LogTag} <color=orange>{label}</color> refitting took <color=cyan>{stopwatch.Elapsed.TotalMilliseconds:0.00} ms</color>. Asset: {asset.Metadata.Id}, Deform: {bodyVariation}");
            }
        }

        public UniTask WaitUntilReadyAsync()
        {
            return UniTask.WhenAll(
                _mainRefittingService.WaitUntilReadyAsync(),
                _altRefittingService.WaitUntilReadyAsync()
            );
        }

        private void ReinitializeServices()
        {
            if (_mainRefittingService is RsRefittingService refittingService)
            {
                refittingService.Dispose();
            }

            if (useLegacyService || !mainShapesLoader)
            {
                _mainRefittingService = _legacyRefittingService as IRefittingService ?? new NoOpRefittingService();
            }
            else
            {
                _mainRefittingService = new RsRefittingService(mainShapesLoader) { RunAsync = mainAsyncRefit };
            }

            if (_altRefittingService is RsRefittingService altRefittingService)
            {
                altRefittingService.Dispose();
            }

            if (altShapesLoader)
            {
                altRefittingService = new RsRefittingService(altShapesLoader);
                altRefittingService.BlendShapePrefix = "alt.refitting.";
                altRefittingService.RunAsync = altAsyncRefit;
                _altRefittingService = altRefittingService;
            }
            else
            {
                _altRefittingService = new NoOpRefittingService();
            }
        }

        private bool TryGetGenie(out IGenie genie)
        {
            genie = null;
            var genieRef = genieParent.GetComponentInChildren<GenieReference>();
            if (!genieRef)
            {
                return false;
            }

            genie = genieRef.Genie;
            return true;
        }

        private static void SetAvatarTPose(IGenie genie)
        {
            genie.Animator.enabled = false;
            SkeletonBone[] skeleton = genie.Animator.avatar.humanDescription.skeleton;
            Dictionary<string, Transform> bonesByName = genie.SkeletonRoot.GetChildrenByName(includeSelf: true);

            foreach (SkeletonBone bone in skeleton)
            {
                if (!bonesByName.TryGetValue(bone.name, out Transform transform))
                {
                    continue;
                }

                // ignore genie root
                if (transform == genie.Root.transform)
                {
                    continue;
                }

                // set the bone transform to the T-position coming from the human description
                transform.localPosition = bone.position;
                transform.localRotation = bone.rotation;
                transform.localScale = bone.scale;
            }
        }

        private static void SetBlendShapeWeight(IEnumerable<SkinnedMeshRenderer> renderers, string shapeName, float weight)
        {
            if (string.IsNullOrEmpty(shapeName))
            {
                return;
            }

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                if (shapeIndex < 0)
                {
                    continue;
                }

                renderer.SetBlendShapeWeight(shapeIndex, weight);
            }
        }
    }
}
