using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Genies.Looks.Customization.Commands;
using static Genies.Customization.MegaEditor.CustomizationContext;
using System;
using Genies.Analytics;
using System.Collections.Generic;
using Genies.Avatars;
using Genies.Avatars.Behaviors;
using Genies.Avatars.Services;
using Genies.Avatars.Services.Flair;
using Genies.CameraSystem;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Inventory.UIData;
using Genies.MegaEditor;
using Genies.Models;
using Genies.Naf;
using Genies.ServiceManagement;
using Genies.UIFramework.Widgets;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Controller for the customize color view.
    /// </summary>
    public class CustomFlairColorCustomizationController : BaseCustomizationController
    {
        [SerializeField]
        private CustomizeColorView _prefab;
        private CustomizeColorView _customizeColorViewInstance;

        [SerializeField]
        public FlairColorItemPickerDataSource flairColorDataSource;

        /// <summary>
        /// The focus camera to activate and set as active on this customization controller.
        /// </summary>
        public GeniesVirtualCameraCatalog virtualCamera;

        private PictureInPictureController _PictureInPictureController => this.GetService<PictureInPictureController>();
        private IFlairCustomColorPresetService _FlairCustomColorPresetService => this.GetService<IFlairCustomColorPresetService>();
        private IAvatarService _AvatarService => this.GetService<IAvatarService>();

        private VirtualCameraController<GeniesVirtualCameraCatalog> _VirtualCameraController => this.GetService<VirtualCameraController<GeniesVirtualCameraCatalog>>();

        private readonly Dictionary<string,FlairColorPreset> _newCustomPreset = new Dictionary<string, FlairColorPreset>();

        private string _FlairMaterialSlot => string.Empty;
        private GradientColorUiData _previousPreset;

        public override async UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;

            if (_customizeColorViewInstance == null)
            {
                _customizeColorViewInstance = _customizer.View.GetOrCreateViewInLayer("custom-flair-color-view",
                    CustomizerViewLayer.CustomizationEditorFullScreen, _prefab);
            }

            return await UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            _VirtualCameraController.ActivateVirtualCamera(virtualCamera);
            _PictureInPictureController.canBeDisabled = false;
            _PictureInPictureController.Enable();
            _VirtualCameraController.SetFullScreenModeInFocusCameras(true);

             //custom colors initialization
            // Initialize the customize color view based on a preset if exist
            _previousPreset = flairColorDataSource.CurrentLongPressColorData;
            FlairColorPreset presetMetadata = null;

            if (_previousPreset != null)
            {
                switch (CurrentCustomColorViewState)
                {
                    case CustomColorViewState.CreateNew:
                        //generate a new instance of the asset based on the last one selected
                        presetMetadata = NewCustomColorFromPreset(_previousPreset);

                        break;
                    case CustomColorViewState.Edit:
                        //edit the current instance of the asset based on the last one selected
                        presetMetadata = EditCustomColorFromPreset(_previousPreset);
                        break;
                    default:
                        CrashReporter.LogError($"Invalid State to access the flair color pick {CurrentCustomColorViewState}");
                        break;
                }

                _newCustomPreset.Add(flairColorDataSource.FlairAssetType.ToString(), presetMetadata);
            }
            else
            {
                //generate a new instance of the asset based on the last one selected
                presetMetadata = new FlairColorPreset()
                {
                    Guid = System.Guid.NewGuid().ToString(),
                    Colors = new []
                    {
                        Color.black,
                        Color.black,
                        Color.black,
                        Color.black,
                    },
                };

                _newCustomPreset.Add(flairColorDataSource.FlairAssetType.ToString(), presetMetadata);
            }

            var props = new AnalyticProperties();
            if(presetMetadata != null && !string.IsNullOrEmpty(presetMetadata.Guid))
            {
                props.AddProperty("selectedColor", $"{presetMetadata.Guid}");
            }

            AnalyticsReporter.LogEvent(FlairCustomizationController.AnalyticsEventsPerFlairType[flairColorDataSource.FlairAssetType][FlairCustomizationController.AnalyticsActionType.ColorPickerSelected], props);
            //provide only 2 channels
            Color[] initialColors = new[]
            {
                presetMetadata.Colors[0],
                presetMetadata.Colors[1],
            };

            _customizeColorViewInstance.Initialize(initialColors);

            // Add listener to the color picker color change event for regions
            _customizeColorViewInstance.OnColorSelected.AddListener(UpdateFlairColor);
        }

        /// <summary>
        /// Updates the flair color of the avatar without using the command pattern.
        /// </summary>
        /// <param name="color">the given skin color</param>
        private void UpdateFlairColor(Color color)
        {
            var indexSelected = _customizeColorViewInstance.ColorRegionsView.SelectedRegionIndex;
            FlairColorPreset flairColorPreset = _newCustomPreset[flairColorDataSource.FlairAssetType.ToString()];

            //swipe the current color selected
            flairColorPreset.Colors[indexSelected] = color;

            //apply the color directly without a commmand
            switch (flairColorDataSource.FlairAssetType)
            {
                case FlairAssetType.Eyebrows:
                    var colors = new GenieColorEntry[]
                    {
                        new (GenieColor.EyebrowsBase, flairColorPreset.Colors[0]),
                        new (GenieColor.EyebrowsR,    flairColorPreset.Colors[1]),
                        new (GenieColor.EyebrowsG,    flairColorPreset.Colors[2]),
                        new (GenieColor.EyebrowsB,    flairColorPreset.Colors[3]),
                    };

                    CurrentCustomizableAvatar.SetColorsAsync(colors).Forget();
                    break;

                case FlairAssetType.Eyelashes:
                    colors = new GenieColorEntry[]
                    {
                        new (GenieColor.EyelashesBase, flairColorPreset.Colors[0]),
                        new (GenieColor.EyelashesR,    flairColorPreset.Colors[1]),
                        new (GenieColor.EyelashesG,    flairColorPreset.Colors[2]),
                        new (GenieColor.EyelashesB,    flairColorPreset.Colors[3]),
                    };

                    CurrentCustomizableAvatar.SetColorsAsync(colors).Forget();
                    break;

                case FlairAssetType.None: default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override void StopCustomization()
        {
            // Remove the color change listener to prevent memory leaks
            _customizeColorViewInstance.OnColorSelected.RemoveListener(UpdateFlairColor);

            _newCustomPreset.Clear();
            _PictureInPictureController.canBeDisabled = true;
            _PictureInPictureController.Disable();
            _VirtualCameraController.SetFullScreenModeInFocusCameras(false);

            CurrentCustomColorViewState = CustomColorViewState.Normal;
            _customizer.View.SecondaryItemPicker.Hide();
        }

        public override bool HasSaveAction()
        {
            return true;
        }

        public override async UniTask<bool> OnSaveAsync()
        {
            if (CurrentCustomizableAvatar == null)
            {
                CrashReporter.Log($"CustomFlairColorCustomizationController's OnSaveAsync no assigned CurrentCustomizableAvatar. Returning false.", LogSeverity.Error);
                return false;
            }

            try
            {
               FlairColorPreset flairColorPreset = _newCustomPreset[flairColorDataSource.FlairAssetType.ToString()];
               Naf.AvatarDefinition avatarDefinition = await _AvatarService.GetAvatarDefinitionAsync();

               FlairColorPreset customSavedPreset = await _FlairCustomColorPresetService.SaveOrCreateCustomPreset(
                   flairColorDataSource.FlairAssetType.ToString(),
                   avatarDefinition,
                   flairColorPreset.Id,
                   flairColorPreset.Colors);

               if (customSavedPreset != null)
               {
                   //set the custom color
                   var colors = FlairColorItemPickerDataSource.MapToFlairColors(customSavedPreset.Colors, flairColorDataSource.FlairAssetType);
                   ICommand command = new SetNativeAvatarColorsCommand(colors, CurrentCustomizableAvatar);
                   await command.ExecuteAsync(new CancellationTokenSource().Token);

               //refresh the data provider to show newly created custom colors
               flairColorDataSource.Dispose();
               await flairColorDataSource.InitializeAndGetCountAsync(new());

               }
               else
               {
                  CrashReporter.LogError($"Failed to save a custom color on avatar definition");
               }

               return true;
            }
            catch (Exception e)
            {
                CrashReporter.Log($"CustomHairColorCustomizationController's OnSaveAsync with CurrentCustomColorViewState {CurrentCustomColorViewState} had an exception: {e}", LogSeverity.Error);
                return true;
            }
        }

        public override bool HasDiscardAction()
        {
            return true;
        }

        public override UniTask<bool> OnDiscardAsync()
        {
            //Discard the changes and apply the last preset only if we have a selected one
            if(_previousPreset != null)
            {
                UpdateFlairColorUsingCommand(_previousPreset);
            }

            return UniTask.FromResult(true);
        }

        private void UpdateFlairColorUsingCommand(GradientColorUiData colorData)
        {
            var colorArray = FlairColorItemPickerDataSource.SafeGetColorsArray(colorData);
            var colors = FlairColorItemPickerDataSource.MapToFlairColors(colorArray, flairColorDataSource.FlairAssetType);

            ICommand command = new SetNativeAvatarColorsCommand(colors, CurrentCustomizableAvatar);
            command.ExecuteAsync(new CancellationTokenSource().Token);
        }

        public override void Dispose()
        {
            _customizeColorViewInstance.Dispose();
        }

        private FlairColorPreset EditCustomColorFromPreset(GradientColorUiData uiData)
        {
            var colors = FlairColorItemPickerDataSource.SafeGetColorsArray(uiData);
            var copyColors = new Color[colors.Length];
            Array.Copy(colors, copyColors, colors.Length);

            var id = uiData.AssetId.Replace(
                $"{IFlairCustomColorPresetService.ChanelPrefixByType[flairColorDataSource.FlairAssetType.ToString()]}-", "");

            return new FlairColorPreset()
            {
                Id = id,
                Guid = uiData.AssetId,
                Colors = copyColors,
            };
        }

        private FlairColorPreset NewCustomColorFromPreset(GradientColorUiData uiData)
        {
            var colors = FlairColorItemPickerDataSource.SafeGetColorsArray(uiData);
            var copyColors = new Color[colors.Length];
            Array.Copy(colors, copyColors, colors.Length);

            var _guid = Guid.NewGuid().ToString();
            var _customGuid =  $"{flairColorDataSource.FlairAssetType.ToString()}-{_guid}";

            return new FlairColorPreset()
            {
                Id = _guid,
                Guid = _customGuid,
                Colors = copyColors,
            };
        }
    }
}
