using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
    public class TextureMapWrapper
    {
        [SerializeField] private string id;
        [SerializeField] private Texture2D texture;
        
        public string Id
        {
            get => id;
            set => id = value;
        }

        public Texture2D Texture
        {
            get => texture;
            set => texture = value;
        }
    }
}