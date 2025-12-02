using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Models
{
    [Serializable]
    public class TextureMap
    {
        [FormerlySerializedAs("type")] public TextureMapType Type;
        [FormerlySerializedAs("texture")] public Texture2D Texture;
    }
}
