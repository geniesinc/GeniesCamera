using System;
using UnityEngine;

namespace Genies.UI.Animations
{
    //TODO: Add easing
    public class FloatAnimation : IAnimation<float> {
        protected float Delta;
        protected float Start;
        protected float End;

        public float Duration { get; private set; }
        public float Progress { get; private set; }
        public float AnimatedValue { get; private set; }
        public bool IsRunning { get; private set; }
        public Action Callback { get; private set; }

        public virtual void Animate(float start, float end, float duration, Action callback = null) {
            Delta = 0f;
            Duration = duration;
            this.Start = start;
            this.End = end;
            IsRunning = true;
            Callback = callback;
        }

        public virtual void Stop() {
            IsRunning = false;
        }

        public virtual void UpdateAnimation(float dt) {
            if (IsRunning) {
                Delta += dt;
                Progress = Delta / Duration;
                Progress = Mathf.Clamp01(Progress);
                AnimatedValue = Mathf.Lerp(Start, End, Progress);

                if (Progress >= 1f) {
                    Callback?.Invoke();
                    IsRunning = false;
                }
            }
        }
    }
}