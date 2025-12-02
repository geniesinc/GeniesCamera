using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TattooLocationContainer", menuName = "Genies/Tattoos/TattooLocationContainer")]
#endif
    public class TattooLocationContainer : OrderedScriptableObject
    {
        [SerializeField] private string _name;
        [SerializeField] private Vector2 _position;
        [SerializeField] private float _rotation;
        [SerializeField] private float _scale;

        public string Name
        {
            get => _name;
            set => _name = value;
        }
        public Vector2 Position
        {
            get => _position;
            set => _position = value;
        }
        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }
        public float Scale
        {
            get => _scale;
            set => _scale = value;
        }
    }
}
