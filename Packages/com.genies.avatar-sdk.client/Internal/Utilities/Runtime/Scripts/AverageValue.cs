using System;

namespace Genies.Utilities
{
    /// <summary>
    /// Utility class to continuously compute an average value over a fixed range of samples.
    /// </summary>
    public sealed class AverageValue
    {
        /// <summary>
        /// The current value average based on previously added values (use <see cref="AddValue"/>).
        /// </summary>
        public float Value { get; private set; }
        public uint AddedSamples { get; private set; }
        
        public readonly int SampleCount;
        public readonly int SubSampleCount;
        
        private readonly float[] _values;
        private int _count;
        private int _subSampleStartIndex;
        private bool _valueUnset;
        private bool _reachedFullSampleCount;

        public AverageValue(int sampleCount, int subSampleCount)
        {
            if (sampleCount <= 0)
            {
                throw new ArgumentOutOfRangeException($"[{nameof(AverageValue)}] sample count cannot be less than 1");
            }

            if (subSampleCount <= 0)
            {
                throw new ArgumentOutOfRangeException($"[{nameof(AverageValue)}] sub sample count cannot be less than 1");
            }

            if (subSampleCount >= sampleCount)
            {
                throw new ArgumentOutOfRangeException($"[{nameof(AverageValue)}] sub sample count cannot be greater or equal than the sample count");
            }

            Value = 0;
            SampleCount = sampleCount;
            SubSampleCount = subSampleCount;
            _values = new float[sampleCount];
            _count = 0;
            _subSampleStartIndex = 0;
            _valueUnset = true;
            _reachedFullSampleCount = false;
        }

        public void AddValue(float value)
        {
            ++AddedSamples;
            
            // the first time a value is set just set it to the average value
            if (_valueUnset)
            {
                Value = value;
                _valueUnset = false;
            }
            
            _values[_count++] = value;

            if (_reachedFullSampleCount)
            {
                if (_count < SampleCount)
                {
                    return;
                }

                // calculate average and average it with the previous average
                float previousValue = Value;
                CalculateAverage(0, SampleCount);
                Value = 0.5f * (previousValue + Value);
                _count = 0;
                return;
            }
            
            // the first time we reach the full sample count we calculate the average without taking into account the previous average
            if (_count == SampleCount)
            {
                CalculateAverage(0, SampleCount);
                _count = 0;
                _reachedFullSampleCount = true;
                return;
            }
            
            // keep averaging the sub samples
            if (_count - _subSampleStartIndex == SubSampleCount)
            {
                CalculateAverage(_subSampleStartIndex, SubSampleCount);
                _subSampleStartIndex += SubSampleCount;
            }
        }

        private void CalculateAverage(int startIndex, int sampleCount)
        {
            int exclusiveEndIndex = startIndex + sampleCount;
            Value = 0.0f;

            for (int i = startIndex; i < exclusiveEndIndex; ++i)
            {
                Value += _values[i];
            }

            Value /= sampleCount;
        }
    }
}
