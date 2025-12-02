using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Models the fog parameters you'll be able to tune for a given look's scene
    /// </summary>
    [System.Serializable]
    public class SceneFog
    {
        /// <summary>
        /// Whether fog is enabled or not.
        /// </summary>
        public bool FogEnabled;

        /// <summary>
        /// Fog mode to use.
        /// </summary>
        public FogMode FogMode;

        /// <summary>
        /// Color of the fog to use.
        /// </summary>
        [JsonProperty("FogColor")]
        public Color FogColor;

        /// <summary>
        /// Start distance if <see cref="FogMode"/> is linear.
        /// </summary>
        public float FogStartDistance;

        /// <summary>
        /// End distance if <see cref="FogMode"/> is linear.
        /// </summary>
        public float FogEndDistance;

        /// <summary>
        /// Density if <see cref="FogMode"/> is exponential or exponential squared.
        /// </summary>
        public float FogDensity;
    }
}
