using System.Collections.Generic;
using UnityEngine.Events;

namespace Genies.UIFramework
{
    /// <summary>
    /// Interface for building and configuring popup UI components.
    /// Handles the creation and setup of popup instances from configuration data.
    /// </summary>
    public interface IPopupBuilder
    {
        /// <summary>
        /// Creates a popup instance based on the provided configuration.
        /// </summary>
        /// <param name="config">The popup configuration containing layout and content specifications.</param>
        /// <returns>The popup configuration mapper for the created popup instance.</returns>
        PopupConfigMapper CreatePopup(PopupConfig config);
        
        /// <summary>
        /// Configures the values and actions for a popup mapper instance.
        /// </summary>
        /// <param name="mapper">The popup configuration mapper to configure.</param>
        /// <param name="config">The popup configuration containing the values to set.</param>
        /// <param name="closeAction">The action to execute when the popup is closed.</param>
        /// <param name="actions">Optional list of actions to assign to popup buttons.</param>
        void SetConfigValues(PopupConfigMapper mapper, PopupConfig config, UnityAction closeAction, List<UnityAction> actions = null);
    }
}
