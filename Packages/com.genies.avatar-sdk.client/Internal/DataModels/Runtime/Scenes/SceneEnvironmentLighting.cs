using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Models the unity environment lighting as it relates to 'looks' scene tunable parameters
    /// </summary>
    [System.Serializable]
    public class SceneEnvironmentLighting
    {
        /// <summary>
        /// Color of the simulated ambient light from the ground.
        /// </summary>
        [JsonProperty("AmbientGroundColor")]
        public Color AmbientGroundColor;

        /// <summary>
        /// Color of the simulated ambient light from the surrounding.
        /// </summary>
        [JsonProperty("AmbientEquatorColor")]
        public Color AmbientEquatorColor;

        /// <summary>
        /// Color of the simulated ambient light from the sky.
        /// </summary>
        [JsonProperty("AmbientSkyColor")]
        public Color AmbientSkyColor;
    }
}
