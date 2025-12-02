using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Genies.Assets.Services
{
#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Assets Service/Addressable Metadata", fileName = "AddressableMetadata")]
#endif
    public class AddressableMetadata : ScriptableObject
    {
        public string displayName;
        public AssetReference thumbnail;
        public AssetReference asset;
    }
}
