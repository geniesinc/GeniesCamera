using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Similar to Unity's <see cref="AvatarBuilder"/> but with extra utilities specific for our genies.
    /// </summary>
    public static class GenieAvatarBuilder
    {
        private const string HipsBoneName = "Hips";
        
        // state
        private static readonly Dictionary<string, Transform>     BonesByName = new();
        private static readonly List<(Transform, TransformState)> BoneStates  = new();

        /// <summary>
        /// Given a <see cref="IGenie"/> instance it will build a new <see cref="Avatar"/> asset for it that is properly
        /// snapped to ground and hips centered on the XZ plane.
        /// </summary>
        public static Avatar BuildGroundedHumanAvatar(IGenie genie)
        {
            return BuildGroundedHumanAvatar(genie, genie.Animator.avatar.humanDescription);
        }
        
        /// <summary>
        /// Given a <see cref="IGenie"/> instance it will build a new <see cref="Avatar"/> asset for it that is properly
        /// snapped to ground and hips centered on the XZ plane. It will use the provided HumanDescription.
        /// </summary>
        public static Avatar BuildGroundedHumanAvatar(IGenie genie, HumanDescription humanDescription)
        {
            StartBuild(genie.Root.transform, genie.SkeletonRoot);
            
            // set all bones to the T-position defined in the human description and find the hips
            if (!SetTPoseAndFindHips(humanDescription.skeleton, out int hipsIndex, out Transform hipsTransform))
            {
                Debug.LogError($"[{nameof(GenieAvatarBuilder)}] couldn't find the hips bone");
                FinishBuild();
                return null;
            }

            // now that we are on T-pose lets get the renderer bounds
            Bounds bounds = GenieUtilities.GetRendererBounds(genie);
            
            // calculate the new hips position so we are grounded on Y axis and centered on the XZ plane
            Transform hipsParent = hipsTransform.parent;
            Vector3 hipsPosition = humanDescription.skeleton[hipsIndex].position;
            hipsPosition = hipsParent?.TransformPoint(hipsPosition) ?? hipsPosition;                   // transform from hips local space to world
            hipsPosition.x = hipsPosition.z = 0.0f;                                                    // center on XZ plane
            hipsPosition.y -= bounds.min.y;                                                            // ground on Y based on T-pose renderer bounds
            hipsPosition = hipsTransform.parent?.InverseTransformVector(hipsPosition) ?? hipsPosition; // transform from world to local hips space
            
            // update hasTranslationDof so joints can be translated with the Animation Rigging package (chaos mode)
            humanDescription.hasTranslationDoF = true;
            
            // set the new hips position to the skeleton bone in the human description and build a new Avatar asset
            humanDescription.skeleton[hipsIndex].position = hipsPosition;
            Avatar groundedAvatar = AvatarBuilder.BuildHumanAvatar(genie.Root, humanDescription);
            
            // restore previous state and clear state
            FinishBuild();
            
            return groundedAvatar;
        }

        private static bool SetTPoseAndFindHips(SkeletonBone[] skeleton, out int hipsIndex, out Transform hipsTransform)
        {
            bool foundHips = false;
            hipsIndex = -1;
            hipsTransform = null;
            
            for (int i = 0; i < skeleton.Length; ++i)
            {
                SkeletonBone bone = skeleton[i];
                if (!BonesByName.TryGetValue(bone.name, out Transform transform))
                {
                    continue;
                }

                // save currents state so it is restored later
                var boneState = (transform, new TransformState(transform));
                BoneStates.Add(boneState);

                // set the bone transform to the T-position coming from the human description
                transform.localPosition = bone.position;
                transform.localRotation = bone.rotation;
                transform.localScale = bone.scale;

                // if this is the hips bone then save its data for later
                if (bone.name != HipsBoneName)
                {
                    continue;
                }

                hipsIndex = i;
                hipsTransform = transform;
                foundHips = true;
            }
            
            return foundHips;
        }
        
        private static void StartBuild(Transform genieRoot, Transform boneRoot)
        {
            BonesByName.Clear();
            BoneStates.Clear();
            boneRoot.AddChildrenByName(BonesByName, recursive: true, includeSelf: true);
            
            // reset genie root to be at the world origin
            var genieRootState = new TransformState(genieRoot);
            BoneStates.Add((genieRoot, genieRootState));
            genieRoot.SetParent(null, worldPositionStays: false);
            genieRoot.ResetLocalTransform();
        }

        private static void FinishBuild()
        {
            foreach ((Transform transform, TransformState state) in BoneStates)
            {
                state.SetTo(transform);
            }

            BonesByName.Clear();
            BoneStates.Clear();
        }
    }
}