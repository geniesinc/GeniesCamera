using System.Collections.Generic;
using UnityEngine.Events;

namespace Genies.UIFramework
{
    /// <summary>
    /// Interface for managing the popup system within the UI framework.
    /// Provides methods for showing, hiding, and configuring popups throughout the application.
    /// </summary>
    public interface IPopupSystem
    {
        /// <summary>
        /// Shows a popup with the specified configuration and action handlers.
        /// </summary>
        /// <param name="config">The popup configuration containing layout, content, and styling information.</param>
        /// <param name="actions">List of actions to be assigned to the popup buttons in order.</param>
        /// <returns>The popup configuration mapper for the created popup.</returns>
        PopupConfigMapper Show(PopupConfig config, List<UnityAction> actions);
        
        /// <summary>
        /// Shows a popup using a predefined popup type and action handlers.
        /// </summary>
        /// <param name="type">The predefined popup type to display.</param>
        /// <param name="actions">List of actions to be assigned to the popup buttons in order.</param>
        /// <returns>The popup configuration mapper for the created popup.</returns>
        PopupConfigMapper Show(PopupType type, List<UnityAction> actions);
        
        /// <summary>
        /// Hides the most recently shown popup.
        /// </summary>
        void HideLatest();
        
        /// <summary>
        /// Hides all currently visible popups.
        /// </summary>
        void HideAll();

        /// <summary>
        /// Retrieves the persistent configuration for a specific popup type.
        /// </summary>
        /// <param name="type">The popup type to get the configuration for.</param>
        /// <returns>The popup configuration for the specified type, or null if not found.</returns>
        PopupConfig GetPersistentConfigByType(PopupType type);
    }
}
