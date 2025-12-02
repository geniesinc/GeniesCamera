using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    /// <summary>
    /// Animates a float value. This class is inspired on the animation CSS property: https://cssreference.io/property/animation/
    /// Go to that link to understand how each parameter will work
    /// </summary>
    [Serializable]
    public struct ValueAnimation
    {
        public enum Direction
        {
            Normal = 0,
            Reverse = 1,
            Alternate = 2,
            AlternateReverse = 3,
        }

        // configuration
        [FormerlySerializedAs("startValue")] public float StartValue;
        [FormerlySerializedAs("endValue")] public float EndValue;
        [FormerlySerializedAs("duration")] public float Duration;
        [FormerlySerializedAs("timingFunction")] public AnimationCurve TimingFunction;
        [FormerlySerializedAs("delay")] public float Delay;
        [FormerlySerializedAs("iterationCount")] public int IterationCount;
        [FormerlySerializedAs("direction")] public Direction AnimationDirection;

        /// <summary>
        /// Returns the value that corresponds to the given time. It will be in the range of [startValue, endValue] except if the timingFunction is out of the [0, 1] range.
        /// Negative time will be clamped to 0.
        /// </summary>
        public float GetValue(float time)
        {
            if (IterationCount == 0 || time < Delay)
            {
                return TimingFunction.Evaluate(StartValue);
            }

            // apply delay and clamp time to be >=0
            time -= Delay;
            if (time < 0.0f)
            {
                time = 0.0f;
            }

            // the interpolation value must be in the range [0, 1] where 0 is the start and 1 is the end of an iteration
            float interpolation = time / Duration;

            // calculate current iteration index and modify the interpolation accordingly
            int iterationIndex = (int) interpolation;
            if (iterationIndex >= IterationCount && IterationCount > 0)
            {
                interpolation = 1.0f; //we surpassed the number of iterations so we set the animation in a finished state
            }
            else
            {
                interpolation %= 1.0f; // extract the non-decimal part so we get the interpolation from 0 to 1 for the current iteration
            }

            // transform the interpolation based on the direction
            interpolation = AnimationDirection switch
            {
                Direction.Normal           => interpolation,
                Direction.Reverse          => 1.0f - interpolation,
                Direction.Alternate        => iterationIndex % 2 == 0 ? interpolation        : 1.0f - interpolation,
                Direction.AlternateReverse => iterationIndex % 2 == 0 ? 1.0f - interpolation : interpolation,
                _ => interpolation,
            };

            // transform the interpolation with the timing function and calculate the final value
            interpolation = TimingFunction.Evaluate(interpolation);
            return StartValue + interpolation * (EndValue - StartValue);
        }

        /// <summary>
        /// Performs the configured animation calling the provided callback on every animation frame with the resulting value
        /// for that frame.
        /// </summary>
        public async UniTask StartAnimationAsync(Action<float> updateCallback,
            PlayerLoopTiming timing = PlayerLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            if (updateCallback is null)
            {
                return;
            }

            float time = 0.0f;
            float totalDuration = Delay + (IterationCount < 0 ? float.PositiveInfinity : IterationCount * Duration);

            while (time < totalDuration && !cancellationToken.IsCancellationRequested)
            {
                updateCallback(GetValue(time));
                await UniTask.Yield(timing);
                time += Time.deltaTime;
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                updateCallback(GetValue(time));
            }
        }
    }
}
