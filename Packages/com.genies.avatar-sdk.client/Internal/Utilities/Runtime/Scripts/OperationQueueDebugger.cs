using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Genies.CrashReporting.Helpers;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Genies.Utilities
{
    /// <summary>
    /// Set the <see cref="Enabled"/> and <see cref="DebugEnqueueing"/> properties to debug the operation enqueueing and dispatching
    /// performed by the <see cref="OperationQueue"/>. Only available on the Unity Editor and development builds.
    /// </summary>
    public static class OperationQueueDebugger
    {
        /// <summary>
        /// Enable this to output verbose logging from the use of the <see cref="OperationQueue"/>.
        /// Only available on the Unity editor and development builds.
        /// </summary>
        public static bool Enabled = false;
        
        /// <summary>
        /// Enable this to also log enqueuing operations (it can be too much logs sometimes).
        /// </summary>
        public static bool DebugEnqueueing = false;

        /**
         * The following methods are conditionally excluded by the compiler when not on the unity editor or a development build.
         * Which means that they have no overhead on those environments.
         */

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugEnqueuedOperation(uint operationId, string timing)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugEnqueuedOperationImp(operationId, timing);
#endif
        }
        
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugTimingDispatchStart(string timing, float targetDeltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugTimingDispatchStartImp(timing, targetDeltaTime);
#endif
        }
        
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugDispatchingOperation()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugDispatchingOperationImp();
#endif
        }
        
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugDispatchedOperation(uint operationId, float operationTime, float targetDeltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugDispatchedOperationImp(operationId, operationTime, targetDeltaTime);
#endif
        }
        
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DebugFrameEnded(int dispatchCount, float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugFrameEndedImp(dispatchCount, deltaTime);
#endif
        }
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly OperationCost[] _operationCosts = Enum.GetValues(typeof(OperationCost)) as OperationCost[];
        private static readonly Dictionary<uint, (string link, string stackTrace)> _operationInfos = new();
        private static bool _isDispatching;
        private static string _frameStartedMsg;
        
        private static void DebugEnqueuedOperationImp(uint operationId, string timing)
        {
            if (!Enabled)
                return;
            
            // get the stack trace and operation link
            var stackTrace = new StackTrace(skipFrames: 3, fNeedFileInfo: true);
            
            StackFrame stackFrame = GetFirstValidStackFrame(stackTrace);
            string filePath = stackFrame.GetFileName();
            string fileName = Path.GetFileName(filePath);
            int line = stackFrame.GetFileLineNumber();
            string operationLink = $"<a href=\"{filePath}\" line=\"{line}\">{fileName}:{line}</a>";
            
            string stackTraceText = stackTrace.GetCleanStackTraceWithFileLinks(ignoreFramesWithoutFileInfo: true);
            _operationInfos[operationId] = (operationLink, stackTraceText);
            
            if (!DebugEnqueueing)
                return;
            
            // log the enqueueing operation
            string enqueueingMsg = _isDispatching ? "    enqueued while dispatching" : " enqueued";
            Debug.Log($"{GetDebugFrameTag()}<color=brown>{enqueueingMsg} {GetOperationIdText(operationId)} ({operationLink}) for {GetTimingText(timing)}</color>\nStackTrace:\n{stackTraceText}");
        }

        private static void DebugTimingDispatchStartImp(string timing, float targetDeltaTime)
        {
            // only log the timing dispatch start when at least one operation is going to be dispatched
            if (!Enabled)
                return;
            
            float availableTime = 1000.0f * Mathf.Max(0.0f, OperationQueueRuntime.GetAvailableFrameTime(targetDeltaTime) - OperationQueueRuntime.FrameUsedTime);
            Debug.Log($"{GetDebugFrameTag()} dispatching at {GetTimingText(timing)} ({availableTime:0.0} ms available):");
        }

        private static void DebugDispatchingOperationImp()
        {
            _isDispatching = true;
        }

        private static void DebugDispatchedOperationImp(uint operationId, float operationTime, float targetDeltaTime)
        {
            _isDispatching = false;

            if (!Enabled)
            {
                _operationInfos.Remove(operationId);
                return;
            }
            
            // this should never happen but just in case
            if (!_operationInfos.TryGetValue(operationId, out var info))
            {
                Debug.LogError($"{GetDebugFrameTag()}    <color=red>failed to log dispatched operation {GetOperationIdText(operationId)} because its enqueuing was not debugged</color>");
                return;
            }
            
            _operationInfos.Remove(operationId);
            
            string totalColor = OperationQueueRuntime.FrameUsedTime > OperationQueueRuntime.GetAvailableFrameTime(targetDeltaTime) ? "red" : "green";
            float frameUsedTime = OperationQueueRuntime.FrameUsedTime * 1000.0f;
            operationTime *= 1000.0f;
            
            // some fancy hardcoded log formatting to make it pretty :)
            string log = $"{GetDebugFrameTag()}    dispatched {GetOperationIdText(operationId)} in <color=cyan>{operationTime:0.0} ms</color>";
            log += new string('.', Mathf.Max(2, 152 - log.Length));
            log += $"Accumulated: <color={totalColor}>{frameUsedTime:0.0} ms</color>";
            log += new string('.', Mathf.Max(2, 200 - log.Length + totalColor.Length));
            log += $"{info.link}\nStackTrace:\n{info.stackTrace}";
            
            Debug.Log(log);
        }
        
        private static void DebugFrameEndedImp(int dispatchCount, float deltaTime)
        {
            if (!Enabled || dispatchCount <= 0)
                return;
            
            string averageOperationCosts = "Average operation times:";
            foreach (OperationCost cost in _operationCosts)
            {
                if (cost is OperationCost.Unknown)
                    continue;
                
                float average = 1000.0f * OperationQueueRuntime.GetEstimatedOperationTime(cost);
                averageOperationCosts += $"\n{cost} cost: <color=cyan>{average:0.0} ms</color>";
            }
            
            float fps = 1.0f / deltaTime;
            deltaTime *= 1000.0f;
            Debug.Log($"{GetDebugFrameTag()} <color=blue>---------- FRAME END: Dispatched {dispatchCount} operations | DeltaTime: {deltaTime:0.0} ms ({fps:0} fps) ----------</color>\n{averageOperationCosts}\n");
        }
        
        private static string GetDebugFrameTag()
        {
            return $"<color=magenta>[{nameof(OperationQueueDebugger)}<color=blue>:{OperationQueueRuntime.CurrentFrame}</color>]</color>";
        }

        public static string GetOperationIdText(uint operationId)
        {
            return $"<color=yellow>{operationId:00000}</color>";
        }

        private static string GetTimingText(string timing)
        {
            return $"<color=orange>{timing}</color>";
        }

        private static StackFrame GetFirstValidStackFrame(StackTrace trace)
        {
            for (int i = 0; i < trace.FrameCount; ++i)
            {
                StackFrame frame = trace.GetFrame(i);
                if (!string.IsNullOrEmpty(frame.GetFileName()))
                    return frame;
            }
            
            return null;
        }
#endif
    }
}
