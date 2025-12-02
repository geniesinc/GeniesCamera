using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
    public struct UgcSkin
    {
        [SerializeField] private string id;
        [SerializeField] private string guid;
        [SerializeField] private int subElements;

        public string Id
        {
            get => id;
            set => id = value;
        }
        
        public string Guid
        {
            get => guid;
            set => guid = value;
        }
        
        public int SubElements
        {
            get => subElements;
            set => subElements = value;
        }
    }
}