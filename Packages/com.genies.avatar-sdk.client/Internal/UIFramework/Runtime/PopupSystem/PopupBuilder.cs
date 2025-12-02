using System.Collections.Generic;
using Genies.CrashReporting;
using UnityEngine;
using UnityEngine.Events;

namespace Genies.UIFramework
{
    public class PopupBuilder : IPopupBuilder
    {
        private const string PopupsPrefix = "Popup";

        // Popups canvas
        private Transform _rootCanvas;
        private readonly string _popupsPath;

        public PopupBuilder(Transform canvas, string popupsPath = "Popups/")
        {
            _rootCanvas = canvas;
            _popupsPath = popupsPath;
        }

        /// <summary>
        /// Instantiates the popup based on a PopupConfig
        /// </summary>
        /// <param name="config">A popup config for configuring the UI</param>
        /// <returns>The insted popup's PopupConfigMapper</returns>
        public PopupConfigMapper CreatePopup(PopupConfig config)
        {
            // Load prefab
            var popupPrefab = Resources.Load($"{_popupsPath}{PopupsPrefix}{config.PopupLayout}");

            if (popupPrefab == null)
            {
                Logger($"Could not load prefab of name {config.PopupLayout} at '{_popupsPath}'");
                return null;
            }

            // Instantiate
            GameObject instedPopup = GameObject.Instantiate(popupPrefab, _rootCanvas) as GameObject;

            if (instedPopup.GetComponent<PopupConfigMapper>() == null)
            {
                Logger($"Insted popup with layout {config.PopupLayout} has no mapper");

                // Destroy the popup
                GameObject.Destroy(instedPopup);

                return null;
            }

#if UNITY_EDITOR
            Debug.Log($"[PopupBuilder] Instantiated a new popup: {config.Header}");
#endif

            // Return the new mapper
            return instedPopup.GetComponent<PopupConfigMapper>();
        }

        /// <summary>
        /// Sets the config and actions of the given popup. Can be used to modify these on-the-go.
        /// </summary>
        /// <param name="mapper">The mapper of the popup we wish to modify</param>
        /// <param name="config">A popup config for configuring the UI</param>
        /// <param name="closeAction">The action to take for closing this popup</param>
        /// <param name="actions">Nullable ordered list of actions for each button</param>
        public void SetConfigValues(PopupConfigMapper mapper, PopupConfig config, UnityAction closeAction, List<UnityAction> actions = null)
        {
            if (mapper == null)
            {
                Logger($"Tried to set config values for a null mapper. Cancelling.");
                return;
            }

            // Set up the popup
            mapper.Setup(config, closeAction, actions);
        }

        private void Logger(string message)
        {
#if UNITY_EDITOR
            Debug.LogError($"[PopupBuilder] {message}");
#endif
        }
    }
}
