using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace Genies.UIFramework
{
    public class PopupSystem : IPopupSystem
    {
        // Configs
        private List<ScriptablePopupConfig> _configs;
        private Dictionary<PopupType, ScriptablePopupConfig> _configsByType;

        // Active popups list
        private List<PopupConfigMapper> _popupsList = new List<PopupConfigMapper>();

        // Builder
        private IPopupBuilder _builder;

        public PopupSystem(Transform popupCanvas, List<ScriptablePopupConfig> persistentConfigs)
        {
            // Construct the popup builder
            _builder = new PopupBuilder(popupCanvas);

            // Save locally our persistent configs
            _configs = persistentConfigs;

            // Populate configs by type dictionary
            _configsByType = DictionaryUtils.ToDictionaryGraceful(_configs, m => m.PopupType, m => m);
        }

        /// <summary>
        /// Show a popup based on a pre-populated popupConfig
        /// </summary>
        /// <param name="config">A popup config for configuring the UI</param>
        /// <param name="actions">Ordered list of actions for each button</param>
        public PopupConfigMapper Show(PopupConfig config, List<UnityAction> actions)
        {
            if (config == null)
            {
                Logger("Tried showing a popup with a null config. Cancelling popup.");
                return default;
            }

            // Popup creation
            var mapper = _builder.CreatePopup(config);

            // Popup setup and layering update
            SetupPopup(mapper, config, actions);

            return mapper;
        }

        /// <summary>
        /// Show a popup based on a generic popupType's config
        /// </summary>
        /// <param name="type">A popupType to grab a base popupConfig from</param>
        /// <param name="actions">Ordered list of actions for each button</param>
        public PopupConfigMapper Show(PopupType type, List<UnityAction> actions)
        {
            PopupConfig config = GetPersistentConfigByType(type);

            if (config == null)
            {
                Logger($"Could not find persistent config for type {type}. Cancelling popup.");
                return default;
            }

            // Popup creation
            var mapper = _builder.CreatePopup(config);

            // Popup setup and layering update
            SetupPopup(mapper, config, actions);

            return mapper;
        }

        /// <summary>
        /// Hides the latest created popup. It removes the latest popup, destroys its gameobject, and updates the layering.
        /// </summary>
        public void HideLatest()
        {
            int lastIndex = _popupsList.Count - 1;

            if (lastIndex >= 0 && _popupsList[lastIndex] != null)
            {
                PopupConfigMapper popup = _popupsList[lastIndex];
                _popupsList.RemoveAt(lastIndex);
                DestroyPopup(popup);
            }
        }

        /// <summary>
        ///  Hides all currently active popups. It removes all popups, destroys their gameobjects, and updates the layering.
        /// </summary>
        public void HideAll()
        {
            foreach (var popup in _popupsList)
            {
                DestroyPopup(popup);
            }
            _popupsList.Clear();
        }

        /// <summary>
        /// Get a generic popupType's config. Can be used to edit the config before using it in a Show()
        /// </summary>
        /// <param name="type">The popupType</param>
        /// <returns>The matching PopupConfig</returns>
        public PopupConfig GetPersistentConfigByType(PopupType type)
        {
            // Check that it exists
            if (!_configsByType.ContainsKey(type))
            {
                return null;
            }

            // Get persistent config
            var config = _configsByType[type].PopupConfig;

            // Create a new matching config (to not edit the original)
            PopupConfig newConfig = new PopupConfig(config.PopupLayout, config.Header, config.Content, config.PlaceholderInputFieldText,
                config.TopImage, config.ButtonLabels, config.HasCloseButton, PopupStyle.Default);

            return newConfig;
        }

        private void SetupPopup(PopupConfigMapper mapper, PopupConfig config, List<UnityAction> actions = null)
        {
            // Confirm that the mapper isn't null
            if (mapper == null)
            {
                Logger($"Received a null mapper. Cancelling popup setup for {config.Header} popup.");
                return;
            }

            // Set the close action to use HideLatest
            var closeAction = new UnityAction(HideLatest);

            // Popup configuration
            _builder.SetConfigValues(mapper, config, closeAction, actions);

            // Add to our popup list
            _popupsList.Add(mapper);

            // Popup layering update
            UpdatePopupLayering();
        }

        private void UpdatePopupLayering()
        {
            int layer = 1;
            for (int i = 0; i < _popupsList.Count; i++)
            {
                PopupConfigMapper popup = _popupsList[i];
                Canvas popupCanvas = popup.GetComponent<Canvas>();
                popupCanvas.sortingOrder = layer++;

                // Activate only the last one's backgroundBlocker
                bool isTopPopup = i == _popupsList.Count - 1;
                popup.ActivateBackgroundBlocker(isTopPopup);
            }
        }

        private void DestroyPopup(PopupConfigMapper mapper)
        {
            mapper.ClearListeners();
            GameObject.Destroy(mapper.gameObject);
            UpdatePopupLayering();
        }

        private void Logger(string message)
        {
#if UNITY_EDITOR
            Debug.LogError($"[PopupSystem] {message}");
#endif
        }
    }
}
