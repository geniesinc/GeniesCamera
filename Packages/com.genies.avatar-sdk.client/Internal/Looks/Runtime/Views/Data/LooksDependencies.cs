using System;
using Genies.Animations.Model;
using UnityEngine;

namespace Genies.Looks.Core.Data
{
    /// <summary>
    /// Container class that holds all the dependencies required for look views to function properly.
    /// This class encapsulates animation components and camera services needed for real-time look rendering and interaction.
    /// </summary>
    [Serializable]
    public class LooksDependencies
    {
        [SerializeField] private AnimatorSwitcherComponents _switcherComponents;
        [SerializeField] private VirtualCameraService _virtualCameraService;

        /// <summary>
        /// Gets or sets the virtual camera service that manages camera controllers for look presentation.
        /// This service handles different camera types including animation cameras and general focus cameras.
        /// </summary>
        public VirtualCameraService VirtualCameraService
        {
            get => _virtualCameraService;
            set => _virtualCameraService = value;
        }

        /// <summary>
        /// Gets or sets the animator switcher components used for animation management in look views.
        /// These components handle animation playback, transitions, and runtime animation switching.
        /// </summary>
        public AnimatorSwitcherComponents SwitcherComponents
        {
            get => _switcherComponents;
            set => _switcherComponents = value;
        }
    }
}
