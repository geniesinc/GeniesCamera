using System;
using Cysharp.Threading.Tasks;
using Screen = UnityEngine.Screen;

namespace Genies.Utilities
{
    public sealed partial class OperationQueue
    {
        /// <summary>
        /// The default target frame rate that will be used for the statically enqueued operations and newly created
        /// <see cref="OperationQueue"/> instances.
        /// </summary>
        public static float DefaultTargetFrameRate
        {
            get
            {
                // initialize to screen refresh rate by default
                if (_defaultTargetFrameRate < 0)
                {
                    _defaultTargetFrameRate = (int)Screen.currentResolution.refreshRateRatio.value;
                }

                return _defaultTargetFrameRate;
            }
            
            set
            {
                _defaultTargetFrameRate = value < 0 ? 0 : value;
                
                if (!_initialized)
                {
                    return;
                }

                foreach (OperationQueue queue in _playerLoopTimingQueues)
                {
                    queue.TargetFrameRate = value;
                }
            }
        }
        
        /// <summary>
        /// <see cref="OperationQueue"/> instance used for <see cref="EnqueueAsync()"/> calls.
        /// </summary>
        public static OperationQueue DefaultOperationQueue;
        
        private static OperationQueue[] _playerLoopTimingQueues;
        private static float _defaultTargetFrameRate = -1;
        private static bool _initialized;
        
        /// <summary>
        /// Invoke and await his method before you perform a single threaded operation to enqueue it. Uses the current
        /// <see cref="DefaultOperationQueue"/>.
        /// <br/><br/>
        /// IMPORTANT: it is assumed that this method is always called within the main thread.
        /// </summary>
        public static UniTask EnqueueAsync(OperationCost cost = OperationCost.Unknown)
        {
            Initialize();
            return DefaultOperationQueue.EnqueueOperationAsync(cost);
        }
        
        /// <summary>
        /// Invoke and await his method before you perform a single threaded operation to enqueue it. The method will always return
        /// at the given <see cref="PlayerLoopTiming"/>.
        /// <br/><br/>
        /// IMPORTANT: it is assumed that this method is always called within the main thread.
        /// </summary>
        public static UniTask EnqueueAsync(PlayerLoopTiming timing, OperationCost cost = OperationCost.Unknown)
        {
            Initialize();
            OperationQueue queue = _playerLoopTimingQueues[(int)timing];
            return queue.EnqueueOperationAsync(cost);
        }

        private static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            
            // initialize one queue for each available PlayerLoopTiming
            PlayerLoopTiming[] timings = Enum.GetValues(typeof(PlayerLoopTiming)) as PlayerLoopTiming[];
            _playerLoopTimingQueues = new OperationQueue[timings.Length];

            foreach (PlayerLoopTiming timing in timings)
            {
                var frameTiming = new PlayerLoopFrameTiming(timing);
                _playerLoopTimingQueues[(int)timing] = new OperationQueue(frameTiming);
            }
            
            // use LastPostLateUpdate by default
            DefaultOperationQueue = _playerLoopTimingQueues[(int)PlayerLoopTiming.LastPostLateUpdate];
        }
    }
}
