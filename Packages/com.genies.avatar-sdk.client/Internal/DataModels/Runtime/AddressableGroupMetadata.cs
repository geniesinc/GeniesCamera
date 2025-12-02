using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Genies.Assets.Services
{
#if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Assets Service/Addressable Group Metadata", fileName = "AddressableGroupMetadata")]
#endif
    public class AddressableGroupMetadata : ScriptableObject
    {
        public string displayName;
        public AssetReference thumbnail;
        public List<AddressableGroupMetadata> children;
        public List<AddressableMetadata> assets;
    }
}
