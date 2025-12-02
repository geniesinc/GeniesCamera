using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
    public class UgcElement
    {
        [SerializeField] private string id;
        [SerializeField] private UgcSkin[] skins;
        
        public string Id
        {
            get => id;
            set => id = value;
        }
        
        public UgcSkin[] Skins
        {
            get => skins;
            set => skins = value;
        }
    }
}