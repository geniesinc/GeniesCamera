using UnityEngine;

namespace Genies.Avatars
{
    public class FlairColorPreset
    {
        public string FlairType { get; set;  } = null;
        public string Guid { get; set;  } = null;

        /// <summary>
        /// a guid without the prefix property (dependency for use gamefeature API)
        /// </summary>
        public string Id { get; set;  } = null;
        public Color[] Colors { get; set;  } = {Color.black, Color.black, Color.black, Color.black};
    }
}
