using System.Collections.Generic;
using Newtonsoft.Json;

namespace Genies.Models
{
    /// <summary>
    /// Models the 'look' scene lighting parameters
    /// </summary>
    /// <remarks>Using HtmlString "#RRGGBBAA" format for Color</remarks>
    [System.Serializable]
    public class SceneLighting
    {
        /// <summary>
        /// Data of the allowed scene lights.
        /// </summary>
        /// <remarks>
        /// We only allow a maximum of 4 Scene Lights active concurrently in each scene, and we use 5
        /// here to provide an easy setup for switching between the Spot Light and Directional Light.
        /// The current lighting rig consists of the following lights:
        /// 1st - a Directional Light (Front) for generating shadow.
        /// 2nd - a Directional Light as the left rim Light for lighting the avatar.
        /// 3rd - a Directional Light as the right rim Light for lighting the avatar.
        /// 4th - usually the background Directional Light, if allowed, will count as the fourth and last allowed light.
        /// 5th - usually the Spot Light, if allowed, will count as the fourth and last allowed light.
        /// </remarks>
        [JsonProperty("SceneLights")]
        public List<SceneLight> SceneLights = new List<SceneLight>(); // Use List rather than a fixed-length Array for the ease of extensions.

        /// <summary>
        /// Settings for smoothing the Spot Light so it is designed once for the scene and adapts to all animation.
        /// </summary>
        [JsonProperty("SpotLightSmoothing")]
        public SceneSpotLightSmoothing SpotLightSmoothing;

        /// <summary>
        /// Environmental lighting using three simulated sources (sky, equator, ground).
        /// </summary>
        [JsonProperty("EnvironmentLighting")]
        public SceneEnvironmentLighting EnvironmentLighting;
        
        /// <summary>
        /// Fog settings.
        /// </summary>
        [JsonProperty("Fog")]
        public SceneFog Fog;
    }
}
