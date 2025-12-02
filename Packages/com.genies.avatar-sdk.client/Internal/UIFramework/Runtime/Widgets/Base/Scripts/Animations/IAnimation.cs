using System;

namespace Genies.UI.Animations
{
    public interface IAnimation<T> {
        bool IsRunning { get; }
        T AnimatedValue { get; }
        void Animate(T start, T end, float duration, Action callback = null);
        void Stop();
        void UpdateAnimation(float dt);
    }
}