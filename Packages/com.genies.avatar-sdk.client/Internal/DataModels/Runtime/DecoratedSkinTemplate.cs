using UnityEngine;

namespace Genies.Models
{
    public class DecoratedSkinTemplate : OrderedScriptableObject
    {
        [SerializeField] private Texture2D _map;
        [SerializeField] private Texture2D _icon;

        public Texture2D Map
        {
            get => _map;
            set => _map = value;
        }
        public Texture2D Icon
        {
            get => _icon;
            set => _icon = value;
        }
    }
}
