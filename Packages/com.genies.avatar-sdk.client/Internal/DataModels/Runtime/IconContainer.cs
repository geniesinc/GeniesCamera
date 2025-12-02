using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
    public class IconContainer : OrderedScriptableObject
    {
        public Texture2D icon;

        public Texture2D _256;
        public Texture2D _512;
        public Texture2D _1024;
        public string assetId;
    }
}