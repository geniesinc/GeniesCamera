using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using Genies.Customization.Framework;
using Genies.Looks.Customization.Commands;
using Genies.Models;
using Genies.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Genies.Customization.MegaEditor.CustomizationContext;
using Genies.Analytics;
using Genies.Avatars;
using Genies.Utilities;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// A customization controller that can be used to modify any chaos mode vector, ie head size
    /// leg size, etc
    /// </summary>
    public class BodyVectorCustomizationController : BaseCustomizationController
    {
        //internal
        private ScrollRect _slidersContainer;
        private List<Slider> _sliders;

        [Serializable]
        public struct BodyVectorSliderConfig
        {
            public string name;
            public string bodyAttributeConfig;
            public float lowerValue;
            public float upperValue;
            public bool showLabel;
            public BodyVectorType vectorType;
        }

        [SerializeField]
        private List<BodyVectorSliderConfig> _sliderConfigs;

        //for instantiation
        private const string _bodyShapeSliderViewId = "body-shape-picker";
        [SerializeField]
        [AssetPath.Attribute(typeof(Slider), AssetPath.PathType.Resources)]
        private string _bodyShapeSliderPrefab;

        [SerializeField]
        [AssetPath.Attribute(typeof(TextMeshProUGUI), AssetPath.PathType.Resources)]
        private string _sliderLabelPrefab;

        [SerializeField]
        [AssetPath.Attribute(typeof(ScrollRect), AssetPath.PathType.Resources)]
        private string _slidersContainerScrollRectPrefab;

        //undo redo
        private GSkelModifierPreset _prevPreset = null;
        private bool _dragging = false;

        // Analytics
        private int[] numModifiedPerSlider;
        public static readonly Dictionary<BodyVectorType, string> AnalyticsEventsPerVectorType =
            new Dictionary<BodyVectorType, string>()
            {
                {BodyVectorType.ArmsThickness, CustomizationAnalyticsEvents.ChaosCustomArmsThickness},
                {BodyVectorType.BellyFullness, CustomizationAnalyticsEvents.ChaosCustomBellyThickness},
                {BodyVectorType.ChestBustline, CustomizationAnalyticsEvents.ChaosCustomChestBustline},
                {BodyVectorType.HipsThickness, CustomizationAnalyticsEvents.ChaosCustomHipsThickness},
                {BodyVectorType.LegsThickness, CustomizationAnalyticsEvents.ChaosCustomLegsThickness},
                {BodyVectorType.NeckThickness, CustomizationAnalyticsEvents.ChaosCustomNeckThickness},
                {BodyVectorType.ShoulderBroadness, CustomizationAnalyticsEvents.ChaosCustomShoulderBroadness},
                {BodyVectorType.WaistThickness, CustomizationAnalyticsEvents.ChaosCustomWaistThickness},
            };

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;

            //slider instantiation
            ScrollRect prefab = AssetPath.Load<ScrollRect>(_slidersContainerScrollRectPrefab);
            _slidersContainer = customizer.View.GetOrCreateViewInLayer(_bodyShapeSliderViewId, CustomizerViewLayer.CustomizationEditor, prefab);
            _slidersContainer.gameObject.SetActive(false);

            InitializeSliders();

            return UniTask.FromResult(true);
        }

        private void InitializeSliders()
        {
            Slider sliderPrefab = AssetPath.Load<Slider>(_bodyShapeSliderPrefab);
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
                BodyVectorSliderConfig config = _sliderConfigs[i];
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
                dragDetector.OnDragStarted += SliderDragStarted;
                dragDetector.OnDragEnded += () => SliderDragEnded(index);
                slider.onValueChanged.AddListener(x => SliderValueChanged(index, x));
            }

            _slidersContainer.verticalNormalizedPosition = 1f;
        }

        public override void StartCustomization()
        {
            _slidersContainer.gameObject.SetActive(true);
            _customizer.View.PrimaryItemPicker.Hide();
            _customizer.View.SecondaryItemPicker.Hide();
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
                    Debug.LogWarning($"{nameof(BodyVectorCustomizationController)} Could not send analytics for {_sliderConfigs[i].vectorType} because event name is found");
                }
            }
        }

        public override void OnUndoRedo()
        {
        }

        private void SliderDragStarted()
        {
            if (_dragging)
            {
                return;
            }

            _dragging = true;
            _prevPreset = CurrentCustomizableAvatar.GetBodyPreset();
        }

        private async void SliderDragEnded(int index)
        {
            numModifiedPerSlider[index]++;

            //Create command for changing body asset
            var preset = CurrentCustomizableAvatar.GetBodyPreset();
            var command = new SetNativeAvatarBodyPresetCommand(preset, _prevPreset, CurrentCustomizableAvatar);

            //Execute the command
            await command.ExecuteAsync(default);

            //register the undo command
            _customizer.RegisterCommand(command);

            _dragging = false;
        }

        private void SliderValueChanged(int index, float val)
        {
            //drag started needs to call first for undo redo
            if (!_dragging)
            {
                SliderDragStarted();
            }

#if UNITY_EDITOR
            if (String.IsNullOrEmpty(_sliderConfigs[index].bodyAttributeConfig))
            {
                Debug.LogWarning($"[{nameof(BodyVectorCustomizationController)}] Body attribute config for {_sliderConfigs[index].name} is missing");
            }
#endif

            CurrentCustomizableAvatar.SetBodyAttribute(_sliderConfigs[index].bodyAttributeConfig, SliderValToVal(index, val));
        }

        private float SliderValToVal(int index, float val)
        {
            BodyVectorSliderConfig config = _sliderConfigs[index];
            return val * (config.upperValue - config.lowerValue) + config.lowerValue;
        }

        private float ValToSliderVal(int index, float val)
        {
            BodyVectorSliderConfig config = _sliderConfigs[index];
            return (val - config.lowerValue) / (config.upperValue - config.lowerValue);
        }

        public override void Dispose() { }
    }
}
