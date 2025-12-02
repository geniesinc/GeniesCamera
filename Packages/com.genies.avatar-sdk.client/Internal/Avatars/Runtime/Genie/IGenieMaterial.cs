using System;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a material instance that can be added to an <see cref="IEditableGenie"/> instance.
    /// </summary>
    public interface IGenieMaterial
    {
        /// <summary>
        /// The material slot ID within the genie that this material belongs to. This should never change for the same material
        /// instance.
        /// </summary>
        string SlotId { get; }

        /// <summary>
        /// The material instance to be used for the slot. This should not be returning a new instance on every invocation.
        /// </summary>
        Material Material { get; }
        
        /// <summary>
        /// Fired every time the material instance has been updated.
        /// </summary>
        event Action Updated;
        
        /// <summary>
        /// Invoked right before the <see cref="IEditableGenie"/> instance is going to apply this genie material.
        /// </summary>
        /// <param name="previousMaterial">The previously applied material instance, useful if you need to copy any configuration from it.</param>
        void OnApplyingMaterial(Material previousMaterial);
    }
}