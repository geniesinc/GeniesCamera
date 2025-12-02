using Genies.Avatars;
using Genies.CameraSystem.Focusable;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Implements <see cref="IFocusable"/> for a GameObject containing a <see cref="UmaGenie"/> component. It is a convenient
    /// adaptation to deprecate the GeniesUmaAvatar with the new Avatars package.
    /// </summary>
    public class GenieFocusable : MonoBehaviour, IFocusable
    {
        public Vector3 TargetViewDirection { get; private set; }

        private IGenie _iGenie;

        public void Initialize(IGenie genie)
        {
            TargetViewDirection = new Vector3(0f, 0f, 1f);
            _iGenie = genie;
        }

        public Bounds GetBounds()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = _iGenie.Renderers[0];

            if (!skinnedMeshRenderer)
            {
                return new Bounds();
            }

            /*
             * This method should return stationary bounds that don't move with animation
             * but does have it's size recalculated. The reason for that is when the camera is focusing
             * on the avatar as it's floating it will not display the floating effect as the camera would be
             * following the avatar.
             */
            Bounds rendererBounds = skinnedMeshRenderer.bounds;
            Vector3 center = rendererBounds.center;
            skinnedMeshRenderer.updateWhenOffscreen = true;
            skinnedMeshRenderer.updateWhenOffscreen = false;
            Vector3 size = rendererBounds.size;

            return new Bounds(center, size);
        }
    }
}
