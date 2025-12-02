using System;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.Avatars.Sdk;
using Genies.CameraSystem;
using Genies.Customization.Framework;
using Genies.Customization.Framework.Navigation;
using Genies.UIFramework;
using UnityEngine;
using Genies.Naf;
using Genies.UI.Widgets;
using Genies.VirtualCamera;

namespace Genies.AvatarEditor.Core
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AvatarSaveOption
#else
    public enum AvatarSaveOption
#endif
    {
        SaveLocallyAndContinue,
        SaveLocallyAndExit,
        SaveRemotelyAndContinue,
        SaveRemotelyAndExit
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct AvatarSaveSettings
#else
    public struct AvatarSaveSettings
#endif
    {
        public AvatarSaveOption SaveOption;
        public string ProfileId;

        public AvatarSaveSettings(AvatarSaveOption saveOption, string profileId = null)
        {
            SaveOption = saveOption;
            ProfileId = profileId;
        }
    }
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarEditingScreen : MonoBehaviour
#else
    public class AvatarEditingScreen : MonoBehaviour
#endif
    {
        public Customizer customizer;
        public NavigationGraph navGraph;
        public NavigationNode avatarRootNode;
        public NavigationNode outfitRootNode;
        public RectTransform editingViewPort;

        public AvatarEditingBehaviour EditingBehaviour { get; private set; }

        [SerializeField]
        private GeniesButton editAvatarButton;

        [SerializeField]
        private GeniesButton editOutfitButton;

        private AvatarEditorMode _currentMode;
        public AvatarEditorMode CurrentMode{get{return _currentMode;}}

        [SerializeField]
        private Spinner previewSpinner;
        public Spinner PreviewSpinner => previewSpinner;

        private AvatarSaveSettings _saveSettings = new AvatarSaveSettings(AvatarSaveOption.SaveRemotelyAndContinue);

        public async UniTask Initialize(GeniesAvatar avatar, Camera camera, VirtualCameraManager virtualCameraManager)
        {
            await UniTask.WaitUntil(() => AvatarEditorInitializer.Instance.Initialized);

            EditingBehaviour = new AvatarEditingBehaviour(this, virtualCameraManager);
            EditingBehaviour.SetSaveSettings(_saveSettings);
            await EditingBehaviour.StartEditing(avatar, camera, navGraph);
            AddListeners();

            await ProcessInitialization();
        }
        public void OnDisable()
        {
            RemoveListeners();
        }

        private void AddListeners()
        {
            if (editAvatarButton != null)
            {
                editAvatarButton.onClick.AddListener(OnGenieButtonClicked);
            }

            if (editOutfitButton)
            {
                editOutfitButton.onClick.AddListener(OnOutfitButtonClicked);
            }
        }

        private void RemoveListeners()
        {
            editAvatarButton.onClick.RemoveAllListeners();
            editOutfitButton.onClick.RemoveAllListeners();
        }

        private void OnGenieButtonClicked()
        {
            ToggleCustomizer(AvatarEditorMode.Avatar);
            EditingBehaviour.CurrentEditMode = AvatarEditorMode.Avatar;
        }

        private void OnOutfitButtonClicked()
        {
            ToggleCustomizer(AvatarEditorMode.Outfit);
            EditingBehaviour.CurrentEditMode = AvatarEditorMode.Outfit;
        }

        public void ToggleCustomizer(AvatarEditorMode mode)
        {
            _currentMode = mode;
            if (mode == AvatarEditorMode.Avatar)
            {
                editAvatarButton.SetButtonSelected(true);

                if (editOutfitButton.IsSelected)
                {
                    editOutfitButton.SetButtonSelected(false);
                }

                EditingBehaviour.SwitchToNode(avatarRootNode);
            }
            else
            {
                editOutfitButton.SetButtonSelected(true);

                if (editAvatarButton.IsSelected)
                {
                    editAvatarButton.SetButtonSelected(false);
                }

                EditingBehaviour.SwitchToNode(outfitRootNode);
            }

        }

        private async UniTask ProcessInitialization()
        {
            await EditingBehaviour.ProcessInitialization();
        }

        /// <summary>
        /// Sets the save behavior for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public void SetSaveOption(AvatarSaveOption saveOption)
        {
            _saveSettings.SaveOption = saveOption;

            // Update the editing behaviour with the new save option
            if (EditingBehaviour != null)
            {
                EditingBehaviour.SetSaveSettings(_saveSettings);
            }
        }

        /// <summary>
        /// Sets the save settings for the avatar editor, including custom profile ID.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public void SetSaveSettings(AvatarSaveSettings saveSettings)
        {
            _saveSettings = saveSettings;

            // Update the editing behaviour with the new save settings
            if (EditingBehaviour != null)
            {
                EditingBehaviour.SetSaveSettings(saveSettings);
            }
        }

        /// <summary>
        /// Sets the save behavior and profile ID for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        /// <param name="profileId">The profile ID to use when saving locally</param>
        public void SetSaveOption(AvatarSaveOption saveOption, string profileId)
        {
            _saveSettings = new AvatarSaveSettings(saveOption, profileId);

            // Update the editing behaviour with the new save settings
            if (EditingBehaviour != null)
            {
                EditingBehaviour.SetSaveSettings(_saveSettings);
            }
        }

        /// <summary>
        /// Gets the current save option.
        /// </summary>
        /// <returns>The current save option</returns>
        public AvatarSaveOption GetSaveOption()
        {
            return _saveSettings.SaveOption;
        }

        /// <summary>
        /// Gets the current save settings.
        /// </summary>
        /// <returns>The current save settings</returns>
        public AvatarSaveSettings GetSaveSettings()
        {
            return _saveSettings;
        }
    }
}
