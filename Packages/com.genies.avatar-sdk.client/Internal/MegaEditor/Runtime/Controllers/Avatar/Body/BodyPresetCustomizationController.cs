using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Analytics;
using Genies.Avatars;
using Genies.Customization.Framework;
using Genies.Customization.Framework.ItemPicker;
using Genies.Looks.Customization.Commands;
using Genies.MegaEditor;
using Genies.UI.Widgets;
using Genies.Utilities;
using UnityEngine;
using static Genies.Customization.MegaEditor.CustomizationContext;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Controller for switching the body type of a unified genie.
    /// </summary>
    public class BodyPresetCustomizationController : BaseCustomizationController, IItemPickerDataSource
    {
        [SerializeField]
        private List<BodyPresetData> _thumbnailData;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _defaultCellSize = new Vector2(87.5f, 95.8f);
            _customizer = customizer;
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BodyTypeCustomizationStarted);

            _customizer.View.PrimaryItemPicker.Show(this).Forget();
            _customizer.View.PrimaryItemPicker.RefreshSelection().Forget();
        }

        public override void StopCustomization()
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.BodyTypeCustomizationStopped);
            _customizer.View.PrimaryItemPicker.Hide();
        }

        public override void Dispose()
        {
            foreach (var data in _thumbnailData)
            {
                data.Dispose();
            }
        }

        public override void OnUndoRedo()
        {
            _customizer.View.PrimaryItemPicker.RefreshSelection().Forget();
        }

        public ItemPickerCtaConfig GetCtaConfig()
        {
            return new ItemPickerCtaConfig(ctaType: CTAButtonType.CustomizeCTA, noneSelectedDelegate: CustomizeSelectedAsync);
        }
        private UniTask<bool> CustomizeSelectedAsync(CancellationToken cancellationToken)
        {
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.ChaosBodyShapeCustomSelectEvent);
            _customizer.GoToEditItemNode();
            return UniTask.FromResult(true);
        }

        public int GetCurrentSelectedIndex()
        {
            var currPreset = CurrentCustomizableAvatar.GetBodyPreset();

            for (int i = 0; i < _thumbnailData.Count; i++)
            {
                var targetPreset = _thumbnailData[i].Preset;
                if (currPreset.EqualsVisually(targetPreset))
                {

                    return i;
                }
            }

            return -1;
        }

        // Pagination support (default implementation - no pagination for body presets yet)
        public bool HasMoreItems => false;
        public bool IsLoadingMore => false;
        public UniTask<bool> LoadMoreItemsAsync(CancellationToken cancellationToken) => UniTask.FromResult(false);

        public UniTask<int> InitializeAndGetCountAsync(CancellationToken cancellationToken)
        {
            return UniTask.FromResult(_thumbnailData.Count);
        }

        public async UniTask<bool> OnItemClickedAsync(int index, ItemPickerCellView clickedCell, bool wasSelected, CancellationToken cancellationToken)
        {
            var data = _thumbnailData[index];
            if (wasSelected)
            {
                return true;
            }

            //Create command for changing body asset
            var command = new SetNativeAvatarBodyPresetCommand(data.Preset, CurrentCustomizableAvatar);

            //Execute the command
            await command.ExecuteAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            //Analytics
            var props = new AnalyticProperties();
            props.AddProperty("currentBodyType", $"{data.PresetName}");
            AnalyticsReporter.LogEvent(CustomizationAnalyticsEvents.UserGenderChangedEvent, props);

            //Register the command for undo/redo
            _customizer.RegisterCommand(command);

            return true;
        }

        public UniTask<bool> InitializeCellViewAsync(ItemPickerCellView view, int index, bool isSelected, CancellationToken cancellationToken)
        {
            var thumbnail = _thumbnailData[index].LoadThumbnail();
            var asGeneric = view as BodyTypeItemPickerCellView;
            if (asGeneric == null)
            {
                return UniTask.FromResult(false);
            }

            asGeneric.thumbnail.sprite = thumbnail;
            asGeneric.SetDebuggingAssetLabel(_thumbnailData[index].PresetName);
            return UniTask.FromResult(true);
        }

        /// <summary>
        /// Ui data for body types.
        /// </summary>
        [Serializable]
        public class BodyPresetData
        {
            [AssetPath.Attribute(typeof(GSkelModifierPreset), AssetPath.PathType.Resources)]
            [SerializeField]
            private string _bodyDataPath;
            [NonSerialized]
            private GSkelModifierPreset _bodyDataSO;

            /// <summary>
            /// The preset
            /// </summary>
            public GSkelModifierPreset Preset
            {
                get
                {
                    if (_bodyDataSO == null)
                    {
                        _bodyDataSO = AssetPath.Load<GSkelModifierPreset>(_bodyDataPath);
                    }

                    return _bodyDataSO;
                }
            }

            /// <summary>
            /// The body preset's name (femaleHeavy, femaleSkinny, etc)
            /// </summary>
            public string PresetName
            {
                get
                {
                    if (_bodyDataSO == null)
                    {
                        _bodyDataSO = AssetPath.Load<GSkelModifierPreset>(_bodyDataPath);
                    }

                    return _bodyDataSO.Name;
                }
            }

            /// <summary>
            /// body variation name (female, male, etc)
            /// </summary>
            public string BodyName
            {
                get
                {
                    if (_bodyDataSO == null)
                    {
                        _bodyDataSO = AssetPath.Load<GSkelModifierPreset>(_bodyDataPath);
                    }

                    return _bodyDataSO.StartingBodyVariation;
                }
            }

            [AssetPath.Attribute(typeof(GSkelModifierPresetIcon), AssetPath.PathType.Resources)]
            [SerializeField]
            private string _uiDataPath;
            private GSkelModifierPresetIcon _uiDataSO;

            private Sprite _thumbnail;

            public Sprite LoadThumbnail()
            {
                if (_thumbnail == null)
                {
                    _uiDataSO = AssetPath.Load<GSkelModifierPresetIcon>(_uiDataPath);
                    _thumbnail = _uiDataSO.Icon;
                }

                return _thumbnail;
            }

            /// <summary>
            /// the gender preset (female, male, androgynous, etc)
            /// </summary>
            public GSkelPresetGender PresetGender
            {
                get
                {
                    if (_uiDataSO == null)
                    {
                        _uiDataSO = AssetPath.Load<GSkelModifierPresetIcon>(_uiDataPath);
                    }

                    return _uiDataSO.FilterGender;
                }
            }

            public void Dispose()
            {
                if (_thumbnail != null)
                {
                    Resources.UnloadAsset(_uiDataSO);
                }
                if (_bodyDataSO != null)
                {
                    Resources.UnloadAsset(_bodyDataSO);
                }
            }
        }
    }
}
