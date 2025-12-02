using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SkeletonModifier", menuName = "Genies/Genie Components/Skeleton Modifier")]
#endif
    public sealed class SkeletonModiferAsset : GenieComponentAsset
    {
        public List<GenieJointModifier> modifiers = new();

        public override GenieComponent CreateComponent()
        {
            return new SkeletonModifier(modifiers);
        }
    }
}
