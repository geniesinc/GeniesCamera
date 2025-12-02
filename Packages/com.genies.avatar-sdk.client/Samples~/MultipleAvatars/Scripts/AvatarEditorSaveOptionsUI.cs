using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Genies.Sdk;

namespace Genies.Sdk.Samples.MultipleAvatars
{
    /// <summary>
    /// UI component that provides a dropdown to control how the Avatar Editor saves avatars.
    /// Uses the save option methods from AvatarSdk.AvatarEditor.
    /// </summary>
    public class AvatarEditorSaveOptionsUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Dropdown for selecting avatar editor save options")]
        [SerializeField] private TMP_Dropdown _saveOptionsDropdown;
        [SerializeField] private TMP_InputField _profileIdInput;

        [SerializeField] private GameObject _saveOptionsGameObject, _profileIdGameObject;

        // Save option enum that matches dropdown indices
        public enum SaveOption
        {
            SaveLocallyAndContinue = 0,
            SaveLocallyAndExit = 1,
            SaveRemotelyAndContinue = 2,
            SaveRemotelyAndExit = 3
        }

        private SaveOption _currentSaveOption = SaveOption.SaveLocallyAndExit;

        private void Awake()
        {
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;

            SetupDropdown();
            WireEvents();
        }

        private void OnDestroy()
        {
            UnwireEvents();
        }

        private void OnUserLoggedIn()
        {
            _saveOptionsGameObject.SetActive(true);
            _profileIdGameObject.SetActive(true);
        }

        private void SetupDropdown()
        {
            _saveOptionsGameObject.SetActive(false);
            _profileIdGameObject.SetActive(false);

            if (_saveOptionsDropdown != null)
            {
                _saveOptionsDropdown.ClearOptions();
                _saveOptionsDropdown.AddOptions(new System.Collections.Generic.List<string>
                {
                    "Save Locally & Continue Editing",
                    "Save Locally & Exit Editor",
                    "Save Remotely & Continue Editing",
                    "Save Remotely & Exit Editor"
                });

                // Set default
                _saveOptionsDropdown.value = (int)SaveOption.SaveLocallyAndExit;
                _currentSaveOption = SaveOption.SaveLocallyAndExit;
                SetSaveOption(_currentSaveOption);
            }
        }

        private void WireEvents()
        {
            if (_saveOptionsDropdown != null)
            {
                _saveOptionsDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }

            if (_profileIdInput != null)
            {
                _profileIdInput.onDeselect.AddListener(OnInputFieldChanged);
            }
        }

        private void UnwireEvents()
        {
            if (_saveOptionsDropdown != null)
            {
                _saveOptionsDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            }

            if (_profileIdInput != null)
            {
                _profileIdInput.onDeselect.RemoveListener(OnInputFieldChanged);
            }
        }

        private void OnDropdownValueChanged(int index)
        {
            _currentSaveOption = (SaveOption)index;

            // Automatically apply the save option when dropdown changes
            ApplyCurrentSaveOption();
        }

        private void OnInputFieldChanged(string input)
        {
            // Automatically apply the save option again when input field changes
            ApplyCurrentSaveOption();
        }

        /// <summary>
        /// Applies the currently selected save option to the Avatar Editor
        /// </summary>
        public async void ApplyCurrentSaveOption()
        {
            try
            {
                Debug.Log($"Setting Avatar Editor save option to: {_currentSaveOption}");

                switch (_currentSaveOption)
                {
                    case SaveOption.SaveLocallyAndContinue:
                        await AvatarSdk.SetEditorSaveLocallyAndContinueAsync(_profileIdInput.text);
                        Debug.Log($"Avatar Editor set to save locally and continue (Profile: {_profileIdInput.text})");
                        break;

                    case SaveOption.SaveLocallyAndExit:
                        await AvatarSdk.SetEditorSaveLocallyAndExitAsync(_profileIdInput.text);
                        Debug.Log($"Avatar Editor set to save locally and exit (Profile: {_profileIdInput.text})");
                        break;

                    case SaveOption.SaveRemotelyAndContinue:
                        await AvatarSdk.SetEditorSaveRemotelyAndContinueAsync();
                        Debug.Log("Avatar Editor set to save remotely and continue");
                        break;

                    case SaveOption.SaveRemotelyAndExit:
                        await AvatarSdk.SetEditorSaveRemotelyAndExitAsync();
                        Debug.Log("Avatar Editor set to save remotely and exit");
                        break;

                    default:
                        Debug.LogWarning($"Unknown save option: {_currentSaveOption}");
                        break;
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set Avatar Editor save option: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the save option programmatically
        /// </summary>
        /// <param name="option">Save option to set</param>
        public void SetSaveOption(SaveOption option)
        {
            _currentSaveOption = option;

            if (_saveOptionsDropdown != null)
            {
                _saveOptionsDropdown.value = (int)option;
            }

            ApplyCurrentSaveOption();
        }
    }
}
