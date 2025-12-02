using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
    public class UgcSplit
    {
        [SerializeField] private string id;
        [SerializeField] private UgcElement[] elements;

        public string Id
        {
            get => id;
            set => id = value;
        }
        
        public UgcElement[] Elements
        {
            get => elements;
            set => elements = value;
        }
    }
}