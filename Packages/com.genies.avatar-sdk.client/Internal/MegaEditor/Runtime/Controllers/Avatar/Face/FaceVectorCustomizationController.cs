using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Avatars;
using Genies.Avatars.Behaviors;
using Genies.Customization.Framework;
using Genies.Inventory;
using Genies.Looks.Customization.Commands;
using Genies.Models;
using Genies.UI;
using Genies.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    public class FaceVectorCustomizationController : BaseCustomizationController
    {
        //internal
        private ScrollRect _slidersContainer;
        private List<Slider> _sliders;

        [SerializeField]
        private GeniesVirtualCameraCatalog _virtualCamera;

        //types
        [SerializeField]
        private AvatarBaseCategory _category;

        [Serializable]
        public struct FaceVectorSliderConfig
        {
            public string name;
            public string bodyAttributeConfig;
            public float lowerValue;
            public float upperValue;
            public bool showLabel;
            public FaceVectorType vectorType;
        }

        [SerializeField]
        private List<FaceVectorSliderConfig> _sliderConfigs;

        //for instantiation
        private const string _faceVectorSliderViewId = "face-vector-sliders";

        [SerializeField]
        [AssetPath.Attribute(typeof(Slider), AssetPath.PathType.Resources)]
        private string _faceVectorSliderPrefab;

        [SerializeField]
        [AssetPath.Attribute(typeof(TextMeshProUGUI), AssetPath.PathType.Resources)]
        private string _sliderLabelPrefab;

        [SerializeField]
        [AssetPath.Attribute(typeof(ScrollRect), AssetPath.PathType.Resources)]
        private string _slidersContainerScrollRectPrefab;
        private GSkelModifierPreset _prevPreset = null;
        private bool _dragging = false;

        // Analytics
        private int[] numModifiedPerSlider;
        public static readonly Dictionary<FaceVectorType, string> AnalyticsEventsPerVectorType =
            new Dictionary<FaceVectorType, string>()
            {
                {FaceVectorType.EyeSize, CustomizationAnalyticsEvents.ChaosCustomEyesSize},
                {FaceVectorType.EyeVerticalPosition, CustomizationAnalyticsEvents.ChaosCustomEyesVerticalPosition},
                {FaceVectorType.EyeSpacing, CustomizationAnalyticsEvents.ChaosCustomEyesSpacing},
                {FaceVectorType.EyeRotation, CustomizationAnalyticsEvents.ChaosCustomEyesRotation},
                {FaceVectorType.BrowThickness, CustomizationAnalyticsEvents.ChaosCustomBrowsThickness},
                {FaceVectorType.BrowLength, CustomizationAnalyticsEvents.ChaosCustomBrowsLength},
                {FaceVectorType.BrowVerticalPosition, CustomizationAnalyticsEvents.ChaosCustomBrowsVerticalPosition},
                {FaceVectorType.BrowSpacing, CustomizationAnalyticsEvents.ChaosCustomBrowsSpacing},
                {FaceVectorType.NoseWidth, CustomizationAnalyticsEvents.ChaosCustomNoseWidth},
                {FaceVectorType.NoseLength, CustomizationAnalyticsEvents.ChaosCustomNoseLength},
                {FaceVectorType.NoseVerticalPosition, CustomizationAnalyticsEvents.ChaosCustomNoseVerticalPosition},
                {FaceVectorType.NoseTilt, CustomizationAnalyticsEvents.ChaosCustomNoseTilt},
                {FaceVectorType.NoseProjection, CustomizationAnalyticsEvents.ChaosCustomNoseProjection},
                {FaceVectorType.LipWidth, CustomizationAnalyticsEvents.ChaosCustomLipsWidth},
                {FaceVectorType.LipFullness, CustomizationAnalyticsEvents.ChaosCustomLipsFullness},
                {FaceVectorType.LipVerticalPosition, CustomizationAnalyticsEvents.ChaosCustomLipsVerticalPosition},
                {FaceVectorType.JawWidth, CustomizationAnalyticsEvents.ChaosCustomJawWidth},
                {FaceVectorType.JawLength, CustomizationAnalyticsEvents.ChaosCustomJawLength},
            };

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;
            //slider instantiation
            ScrollRect prefab = AssetPath.Load<ScrollRect>(_slidersContainerScrollRectPrefab);
            _slidersContainer = customizer.View.GetOrCreateViewInLayer(_faceVectorSliderViewId, CustomizerViewLayer.CustomizationEditor, prefab);
            _slidersContainer.gameObject.SetActive(false);

            InitializeSliders();

            return UniTask.FromResult(true);
        }

        private void InitializeSliders()
        {
            Slider sliderPrefab = AssetPath.Load<Slider>(_faceVectorSliderPrefab);
            TextMeshProUGUI labelPrefab = AssetPath.Load<TextMeshProUGUI>(_sliderLabelPrefab);

            if (_slidersContainer.content.childCount > 0)
            {
                foreach (Transform obj in _slidersContainer.content.transform)
                {
                    Destroy(obj.gameObject);
                }
            }

            for (var i = 0; i < _sliderConfigs.Count; i++)
            {
                FaceVectorSliderConfig config = _sliderConfigs[i];
                if (config.showLabel)
                {
                    TextMeshProUGUI label = Instantiate(labelPrefab, _slidersContainer.content);
                    label.text = config.name;
                }
                Slider slider = Instantiate(sliderPrefab, _slidersContainer.content);
                PointerEventDetector dragDetector = slider.GetComponent<PointerEventDetector>();

                //slider initial value
                var currVal = CurrentCustomizableAvatar.GetBodyAttribute(config.bodyAttributeConfig);
                slider.value = ValToSliderVal(i, currVal);

                var index = i;
                dragDetector.OnDragStarted += () => SliderDragStarted(index);
                dragDetector.OnDragEnded += () => SliderDragEnded(index);
                slider.onValueChanged.AddListener(x => SliderValueChanged(index, x));
            }

            _slidersContainer.verticalNormalizedPosition = 1f;
        }

        public void ResetAllValues()
        {
            foreach (var config in _sliderConfigs)
            {
                CurrentCustomizableAvatar.SetBodyAttribute(config.bodyAttributeConfig, 0);
            }
        }

        public override void StartCustomization()
        {
            CurrentVirtualCameraController.ActivateVirtualCamera(_virtualCamera);
            _slidersContainer.gameObject.SetActive(true);
            _customizer.View.PrimaryItemPicker.Hide();
            _customizer.View.SecondaryItemPicker.Hide();
            CurrentDnaCustomizationViewState = _category;

            InitializeAnalyticsData();
        }

        public override void StopCustomization()
        {
            _slidersContainer.gameObject.SetActive(false);
            SendAnalyticsData();
        }

        private void InitializeAnalyticsData()
        {
            numModifiedPerSlider = new int[_sliderConfigs.Count];
        }

        private void SendAnalyticsData()
        {
            for (var i = 0; i < _sliderConfigs.Count; i++)
            {
                // skip if user hasn't modified the slider option
                if (numModifiedPerSlider[i] == 0)
                {
                    continue;
                }

                if (AnalyticsEventsPerVectorType.TryGetValue(_sliderConfigs[i].vectorType, out var eventName))
                {
                    var props = new AnalyticProperties();
                    props.AddProperty("num_modified", numModifiedPerSlider[i].ToString());
                    AnalyticsReporter.LogEvent(eventName, props);
                }
                else
                {
                    Debug.LogWarning($"{nameof(FaceVectorCustomizationController)} Could not send analytics for {_sliderConfigs[i].vectorType} because event name is found");
                }
            }
        }

        public override void OnUndoRedo()
        {
        }

        private void SliderDragStarted(int index)
        {
            if (_dragging)
            {
                return;
            }

            _dragging = true;
            if (_prevPreset)
            {
                Destroy(_prevPreset);
            }

            _prevPreset = CurrentCustomizableAvatar.GetBodyPreset();
        }

        private async void SliderDragEnded(int index)
        {
            _dragging = false;

            numModifiedPerSlider[index]++;

            //Create command for changing body asset
            var preset = CurrentCustomizableAvatar.GetBodyPreset();
            var command = new SetNativeAvatarBodyPresetCommand(preset, _prevPreset, CurrentCustomizableAvatar);

            //Execute the command
            await command.ExecuteAsync(default);
            Destroy(preset);
        }

        private void SliderValueChanged(int index, float val)
        {
            //drag started needs to call first for undo redo
            if (!_dragging)
            {
                SliderDragStarted(index);
            }

#if UNITY_EDITOR
            if (String.IsNullOrEmpty(_sliderConfigs[index].bodyAttributeConfig))
            {
                Debug.LogWarning($"[{nameof(FaceVectorCustomizationController)}] Body attribute config for {_sliderConfigs[index].name} is missing");
            }
#endif

            CurrentCustomizableAvatar.SetBodyAttribute(_sliderConfigs[index].bodyAttributeConfig, SliderValToVal(index, val));
        }

        private float SliderValToVal(int index, float val)
        {
            FaceVectorSliderConfig config = _sliderConfigs[index];
            return val * (config.upperValue - config.lowerValue) + config.lowerValue;
        }

        private float ValToSliderVal(int index, float val)
        {
            FaceVectorSliderConfig config = _sliderConfigs[index];
            return (val - config.lowerValue) / (config.upperValue - config.lowerValue);
        }

        public override void Dispose()
        {
            //TODO: Dispose all the data refs and sprite cache generated here
        }
    }
}
