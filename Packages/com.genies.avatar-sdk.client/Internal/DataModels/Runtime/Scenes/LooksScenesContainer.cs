using Genies.Components.ShaderlessTools;
using UnityEngine;
//TODO DEPRECATE
namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for LooksScenesContainer.
    /// It holds are the data that the thing will use in the addressable bundles
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "LooksScenesContainer", menuName = "Genies/Looks/LooksScenesContainer", order = 0)]
#endif
    public class LooksScenesContainer : OrderedScriptableObject, IShaderlessAsset
    {
        [SerializeField] private string assetId;
        [SerializeField] private GameObject prefab;
        [SerializeField] private Texture2D thumbnail;
        [SerializeField] private ShaderlessMaterials shaderlessMaterials;

        public string AssetId
        {
            get => assetId;
            set => assetId = value;
        }

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        public Texture2D Thumbnail
        {
            get => thumbnail;
            set => thumbnail = value;
        }

        public ShaderlessMaterials ShaderlessMaterials
        {
            get => shaderlessMaterials;
            set => shaderlessMaterials = value;
        }
    }
}
