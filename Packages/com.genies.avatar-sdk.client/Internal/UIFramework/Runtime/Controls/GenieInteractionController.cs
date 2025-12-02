using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.UIFramework
{
    /// <summary>
    /// Specialized interaction controller for Genie avatar interactions within the UI framework.
    /// Automatically registers itself with the service management system when enabled.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class GenieInteractionController : InteractionController
    {
        /// <summary>
        /// Called when the component becomes enabled. Automatically registers this controller
        /// with the service management system to support different screen contexts that may use
        /// either UserUnifiedGenie or SingleRealtimeLook genie instances.
        /// </summary>
        private void OnEnable()
        {
            this.RegisterSelf();
        }
    }
}
