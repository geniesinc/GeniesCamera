using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars.Sdk
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAnimatorEventBridge : MonoBehaviour
#else
    public class GeniesAnimatorEventBridge : MonoBehaviour
#endif
    {
        public enum AnimEventType
        {
            OnFootstep,
            OnLand
        }

        private readonly Dictionary<AnimEventType, Action<AnimationEvent>> _eventDict = new Dictionary<AnimEventType, Action<AnimationEvent>>()
        {
            { AnimEventType.OnFootstep, null },
            { AnimEventType.OnLand, null }
        };

        public void Subscribe(AnimEventType type, Action<AnimationEvent> callback)
        {
            _eventDict[type] += callback;
        }

        public void Unsubscribe(AnimEventType type, Action<AnimationEvent> callback)
        {
            _eventDict[type] -= callback;
        }

        /// <summary>
        /// Unity Animation Event calls (called from Unity Animations)
        /// </summary>
        /// <param name="animationEvent"></param>
        ///
        private void OnFootstep(AnimationEvent animationEvent)
        {
            _eventDict[AnimEventType.OnFootstep]?.Invoke(animationEvent);
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            _eventDict[AnimEventType.OnLand]?.Invoke(animationEvent);
        }
    }
}
