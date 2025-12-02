using System;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Genie component that automatically snaps the genie to the ground each time the root gets rebuilt. It snaps
    /// locally to the Y origin of the current genie root parent.
    /// </summary>
    [Obsolete("All genies are now automatically snapped to ground in the UmaGenie implementation, this component is no longer needed and it does nothing")]
    public sealed class SnapToGround : GenieComponent
    {
        public override string Name => "Snap To Ground";
        
        /// <summary>
        /// The current value of the height (Y axis) that must be applied to the genie so it is grounded.
        /// </summary>
        public float GroundedHeight { get; private set; }

        /// <summary>
        /// Recalculates the grounded height and applies it to the genie.
        /// </summary>
        public void Reground()
        {
            Debug.LogWarning($"The {nameof(SnapToGround)} genie component has been deprecated as all genies are grounded by default now. Please remove any code using it or adding it to genie instances");
        }
        
        public override GenieComponent Copy()
        {
            return new SnapToGround();
        }

        protected override bool TryInitialize()
        {
            // deprecated
            Debug.LogWarning($"The {nameof(SnapToGround)} genie component has been deprecated as all genies are grounded by default now. Please remove any code using it or adding it to genie instances");
            return false;
        }

        protected override void OnRemoved()
        {
        }
    }
}