using System;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Genies.Utilities
{
    /// <summary>
    /// Internal runtime required to compute frame timings for <see cref="OperationQueue"/> to work.
    /// </summary>
    internal static class OperationQueueRuntime
    {
        private const float _freeTimeOffset = 0.0005f; // gives 0.5 ms offset to the free time calculations
        private const int _averageOperationTimeSampleCount = 100;
        private const int _averageOperationTimeSubSampleCount = 5;

        public static uint CurrentFrame { get; private set; }
        public static float FrameUsedTime => _frameUsedTime;

        /**
         * Static variables updated on each frame by this runtime. We calculate our own timings instead of using Time.unscaledDeltaTime and
         * Time.frameCount because:
         *     1. Causes issues when using PlayerLoopTiming.TimeUpdate, PlayerLoopTiming.FixedUpdate and PlayerLoopTiming.LateFixedUpdate.
         *     2. Doesn't provide the real time values that we need (i.e.: when the frame rate is capped). We need the real time unity
         *        takes to render a frame.
         */
        private static float _frameStartTime;
        private static float _previousFrameDeltaTime;
        private static float _frameUsedTime;
        private static int   _frameDispatchedOperations;
        private static bool  _initialized;

        // average operation times based on their estimated costs
        private static AverageValue[] _averageOperationTimes;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            PlayerLoopSystem root = PlayerLoop.GetCurrentPlayerLoop();

            // create a new extended array for the subsystems
            PlayerLoopSystem[] subSystems = new PlayerLoopSystem[root.subSystemList.Length + 2];

            // insert our custom loop that runs before anything else at the start of each frame
            subSystems[0] = new PlayerLoopSystem
            {
                type = typeof(OperationQueueRuntimeFrameStart),
                updateDelegate = OnFrameStarted
            };

            // insert our custom loop that runs after anything else at the end of each frame
            subSystems[^1] = new PlayerLoopSystem
            {
                type = typeof(OperationQueueRuntimeFrameEnd),
                updateDelegate = OnFrameEnded
            };

            // add the already existing systems in between
            for (int i = 0; i < root.subSystemList.Length; ++i)
            {
                subSystems[i + 1] = root.subSystemList[i];
            }

            // update subsystems array and set the new root
            root.subSystemList = subSystems;
            PlayerLoop.SetPlayerLoop(root);

            // initialize average operation values
            OperationCost[] costs = Enum.GetValues(typeof(OperationCost)) as OperationCost[];
            _averageOperationTimes = new AverageValue[costs.Length];

            foreach (OperationCost cost in costs)
            {
                // we will not compute the average for operation with unkown cost
                if (cost is OperationCost.Unknown)
                {
                    continue;
                }

                _averageOperationTimes[(int)cost] = new AverageValue(_averageOperationTimeSampleCount, _averageOperationTimeSubSampleCount);
            }
        }

        public static bool CanFrameDispatchOperation(float targetDeltaTime, OperationCost cost)
        {
            /**
             * We must always dispatch at least one operation per frame since it could be impossible to meet the target delta time.
             * If we don't do this the operations would stay queued forever in those edgecases.
             */
            if (_frameDispatchedOperations == 0)
            {
                return true;
            }

            return _frameUsedTime < GetAvailableFrameTime(targetDeltaTime) - GetEstimatedOperationTime(cost);
        }

        public static float GetAvailableFrameTime(float targetDeltaTime)
        {
            return targetDeltaTime - _previousFrameDeltaTime - _freeTimeOffset;
        }

        public static void DispatchedOperation(float time, OperationCost cost)
        {
            _frameUsedTime += time;
            ++_frameDispatchedOperations;

            if (cost is not OperationCost.Unknown)
            {
                _averageOperationTimes[(int)cost].AddValue(time);
            }
        }

        public static float GetEstimatedOperationTime(OperationCost cost)
        {
            // get an estimation of the time that the operation will take based on the current average for the given cost
            return cost is OperationCost.Unknown ? 0.0f : _averageOperationTimes[(int)cost].Value;
        }

        public static uint GetOperationCostSamples(OperationCost cost)
        {
            return cost is OperationCost.Unknown ? 0 : _averageOperationTimes[(int)cost].AddedSamples;
        }

        private static void OnFrameStarted()
        {
            ++CurrentFrame;
            _frameStartTime = Time.realtimeSinceStartup;
            _frameUsedTime = 0;
            _frameDispatchedOperations = 0;
        }

        private static void OnFrameEnded()
        {
#if UNITY_EDITOR
            // really annoying that Unity keeps the added player loop systems after playing stopped in the editor
            if (!Application.isPlaying)
                return;
#endif

            /*
             * This is the best approximation of the real time that it took to render the entire frame. We remove the time
             * consumed by the dispatched operations as it shouldn't be taken into account. We also ignore Unity's mainthread
             * sleeping when waiting for target frame rate or vsync, which is what we need to know how much time is available
             * to enqueue operations on the next frame.
             *
             * The only timing missing on this calculation is the time that it takes to output the frame to the display which usually
             * ranges from 0.2 to 0.5 milliseconds depending on the device. I really couldn't find a way to get this timing value from
             * Unity.
             */
            _previousFrameDeltaTime = Time.realtimeSinceStartup - _frameStartTime - _frameUsedTime;

            OperationQueueDebugger.DebugFrameEnded(_frameDispatchedOperations, Time.realtimeSinceStartup - _frameStartTime);
        }

#if UNITY_EDITOR
        public static bool EditorQueuingDisabled { get; private set; }

        public class OperationQueueDebuggerEditorWindow : UnityEditor.EditorWindow
        {
            private static readonly OperationCost[] _operationCosts = Enum.GetValues(typeof(OperationCost)) as OperationCost[];

#if GENIES_INTERNAL
            [UnityEditor.MenuItem("Genies/Operation Queue Debugger")]
#endif
            public static void ShowWindow()
            {
                GetWindow<OperationQueueDebuggerEditorWindow>("Operation Queue Debugger");
            }

            private void OnGUI()
            {
#if DISABLE_OPERATION_QUEUE
                UnityEditor.EditorGUILayout.HelpBox("The Operation Queue feature is currently disabled on this project (DISABLE_OPERATION_QUEUE is defined).", UnityEditor.MessageType.Info);
                return;
#endif

                if (!Application.isPlaying)
                {
                    UnityEditor.EditorGUILayout.HelpBox("Enter play mode to debug the operation queue.", UnityEditor.MessageType.Info);
                    return;
                }

                if (!_initialized)
                {
                    UnityEditor.EditorGUILayout.HelpBox("Runtime has not been initialized yet. It won't initialize until an operation is queued...", UnityEditor.MessageType.Info);
                    return;
                }

                EditorQueuingDisabled = !UnityEditor.EditorGUILayout.Toggle("Enabled", !EditorQueuingDisabled);
                if (EditorQueuingDisabled)
                    return;

                OperationQueue.DefaultTargetFrameRate = UnityEditor.EditorGUILayout.FloatField("Target Frame Rate", OperationQueue.DefaultTargetFrameRate);
                OperationQueueDebugger.Enabled = UnityEditor.EditorGUILayout.Toggle("Debugging", OperationQueueDebugger.Enabled);

                if (OperationQueueDebugger.Enabled)
                    OperationQueueDebugger.DebugEnqueueing = UnityEditor.EditorGUILayout.Toggle("Debug Enqueueing", OperationQueueDebugger.DebugEnqueueing);

                string msg = "\nCurrent operation time estimations based on cost:\n\n";
                foreach (OperationCost cost in _operationCosts)
                {
                    if (cost is OperationCost.Unknown)
                        continue;

                    float average = 1000.0f * GetEstimatedOperationTime(cost);
                    uint samples = GetOperationCostSamples(cost);
                    msg += $"  * {cost} cost:\t{average:0.0} ms\t({samples} samples)\n";
                }

                UnityEditor.EditorGUILayout.HelpBox(msg, UnityEditor.MessageType.None);
            }
        }
#endif
    }

    /// <summary>
    /// Type for the custom player loop system used by <see cref="OperationQueueRuntime"/> to capture frame timings.
    /// </summary>
    public sealed class OperationQueueRuntimeFrameStart { }

    /// <summary>
    /// Type for the custom player loop system used by <see cref="OperationQueueRuntime"/> to capture frame timings.
    /// </summary>
    public sealed class OperationQueueRuntimeFrameEnd { }
}
