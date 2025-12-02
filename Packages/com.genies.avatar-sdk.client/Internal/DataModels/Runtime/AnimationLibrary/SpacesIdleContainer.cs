using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SpacesIdleContainer", menuName = "Genies/AnimationLibrary/SpacesIdleContainer", order = 0)]
#endif
    public class SpacesIdleContainer : AnimationContainer
    {
        [SerializeField] private string[] protocols;
        [SerializeField] private List<ChildAsset> childAssets;

        public string[] ProtocolTags
        {
            get => protocols;
            set => protocols = value;
        }

        public List<ChildAsset> ChildAssets
        {
            get => childAssets ??= new List<ChildAsset>();
            set => childAssets = value;
        }
    }
}
