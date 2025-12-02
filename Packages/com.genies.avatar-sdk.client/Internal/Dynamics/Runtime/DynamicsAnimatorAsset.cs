using Genies.Avatars;
using Genies.Components.Dynamics;
using UnityEngine;

namespace Genies.Dynamics
{
    /// <summary>
    /// Wrapper around <see cref="DynamicsRecipe"/> that allows for creation of a <see cref="DynamicsAnimator"/>
    /// for running Dynamics on Genies Avatars.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DynamicsAnimatorAsset", menuName = "Genies/Dynamics/Dynamics Animation Asset")]
#endif
    public class DynamicsAnimatorAsset : GenieComponentAsset
    {
        public DynamicsRecipe recipe;

        public override GenieComponent CreateComponent()
        {
            return new DynamicsAnimator(recipe);
        }
    }
}
