using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Services;
using Genies.CrashReporting;
using UnityEngine;
using UnityEngine.Serialization;

[assembly: InternalsVisibleTo("Genies.Multiplayer.Sdk")]

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Represents an avatar instance from a user account and provides some basic control functionality.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesAvatarLoader : MonoBehaviour
#else
    public sealed class GeniesAvatarLoader : MonoBehaviour
#endif
    {
        internal const string DefaultGoName = "User Avatar";


        [FormerlySerializedAs("animatorController")] [SerializeField]
        private RuntimeAnimatorController _animatorController;

        [FormerlySerializedAs("overlays")] [SerializeField]
        private List<AvatarOverlay> _overlays = new();

        /// <summary>
        /// Gets the underlying IGenie instance that represents the loaded avatar.
        /// </summary>
        public IGenie Genie => _genie;

        private IGenie _genie;
        private AvatarOverlayController _overlayController;

        /// <summary>
        /// Sets the given <see cref="RuntimeAnimatorController"/> to control the avatar animation.
        /// </summary>
        /// <param name="controller">The RuntimeAnimatorController to apply to the avatar's animator.</param>
        public void SetAnimatorController(RuntimeAnimatorController controller)
        {
            if (_genie?.Animator)
            {
                _genie.Animator.runtimeAnimatorController = controller;
            }

            _animatorController = controller;
        }

        /// <summary>
        /// Adds an overlay to the avatar. Overlays are additional geometry or effects attached to avatar bones.
        /// </summary>
        /// <param name="overlay">The AvatarOverlay component to add to this avatar.</param>
        public void AddOverlay(AvatarOverlay overlay)
        {
            if (!_overlays.Contains(overlay))
            {
                _overlays.Add(overlay);
            }

            _overlayController?.Add(overlay);
        }

        /// <summary>
        /// Removes an overlay from the avatar.
        /// </summary>
        /// <param name="overlay">The AvatarOverlay component to remove from this avatar.</param>
        public void RemoveOverlay(AvatarOverlay overlay)
        {
            _overlays.Remove(overlay);
            _overlayController?.Remove(overlay);
        }

        /// <summary>
        /// Gets the Animator component attached to the avatar.
        /// </summary>
        /// <returns>The avatar's Animator component, or null if no avatar is loaded.</returns>
        public Animator GetAnimator()
        {
            return _genie?.Animator;
        }

        /// <summary>
        /// Gets the collection of SkinnedMeshRenderer components that make up the avatar's visual representation.
        /// </summary>
        /// <returns>A read-only list of SkinnedMeshRenderer components, or null if no avatar is loaded.</returns>
        public IReadOnlyList<SkinnedMeshRenderer> GetRenderers()
        {
            return _genie?.Renderers;
        }

        /// <summary>
        /// Sets up the avatar using the specified parameters and loads the avatar geometry.
        /// This is an async operation that creates and initializes the avatar instance.
        /// </summary>
        /// <param name="definition">Optional avatar definition JSON string. If null, uses default avatar.</param>
        /// <param name="parent">Optional parent Transform for the avatar. If null, uses this GameObject's transform.</param>
        /// <param name="controller">Optional RuntimeAnimatorController to apply to the avatar.</param>
        /// <param name="atlasResolution">Resolution for the texture atlas (default: 512).</param>
        /// <returns>A task that completes when the avatar setup is finished.</returns>
        public async UniTask SetupAvatarAndControllers(string definition = null,
                                                       Transform parent = null,
                                                       RuntimeAnimatorController controller = null,
                                                       int atlasResolution = 512
        )
        {
            // dispose previous avatar if any
            _overlayController?.RemoveAll();
            _genie?.Dispose();

            try
            {
                if (controller != null)
                {
                    _animatorController = controller;
                }

                var geniesAvatarSdkService = await GeniesAvatarsSdk.GetOrCreateAvatarSdkInstance();
                _genie = (await geniesAvatarSdkService.CreateAvatarAsync(definition, parent, atlasResolution))?.Genie;

                // set the current animator controller
                if (_genie != null && _genie.Animator != null)
                {
                    _genie.Animator.runtimeAnimatorController = _animatorController;
                }

                // create the overlay controller and add the overlays
                _overlayController = new AvatarOverlayController(_genie);
                foreach (AvatarOverlay overlay in _overlays)
                {
                    _overlayController.Add(overlay);
                }

                // parent the avatar to this GameObject
                _genie.Root.name = "Genie";
                _genie.Root.transform.SetParent(transform, worldPositionStays: false);
            }
            catch (AggregateException e)
            {
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
        }

        private UniTask<UnifiedGenieController> LoadUnifiedGenieController(Transform parent)
        {
            return AvatarsFactory.CreateEditableGenieAsync(parent: parent);
        }

        private void OnDestroy()
        {
            if (_genie is null)
            {
                return;
            }

            _genie.Dispose();
            _genie = null;
        }
    }
}
