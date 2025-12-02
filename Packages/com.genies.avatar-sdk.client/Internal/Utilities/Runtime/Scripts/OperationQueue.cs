using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Genies.Utilities
{
    /// <summary>
    /// Used to spread heavy main thread operations across multiple frames.
    /// <br/><br/>
    /// Await <see cref="EnqueueOperationAsync"/> before you perform a single threaded operation to enqueue it.
    /// To get the best performance out of this class you should try to enqueue operations that cannot be split
    /// (i.e.: compressing a texture). The smaller computation time required by each single operation the better
    /// job the algorithm will do spreading the workload across frames.
    /// <br/><br/>
    /// Instantiating your own <see cref="OperationQueue"/> is generally not required. It is recommended that you
    /// use the static enqueueing methods instead (<see cref="EnqueueAsync()"/>, <see cref="EnqueueForEndOfFrameAsync"/>).
    /// </summary>
    public sealed partial class OperationQueue
    {
        private static uint _operationId = 0;
        
        /// <summary>
        /// The target frames per second that we this queue will try to maintain when dispatching operations.
        /// </summary>
        public float TargetFrameRate
        {
            get => 1.0f / _targetDeltaTime;
            set => _targetDeltaTime = 1.0f / value;
        }
        
        /// <summary>
        /// The current count of operations queued for execution on this queue.
        /// </summary>
        public int QueueCount => _operations.Count;
        
        public readonly IOperationQueueFrameTiming Timing;
        
        private readonly Queue<Operation> _operations;
        private float _targetDeltaTime;
        private bool _isDispatching;

        public OperationQueue(IOperationQueueFrameTiming timing)
            : this(timing, DefaultTargetFrameRate) { }

        public OperationQueue(IOperationQueueFrameTiming timing, float targetFrameRate)
        {
            OperationQueueRuntime.Initialize();
            Timing = timing;
            TargetFrameRate = targetFrameRate;
            _operations = new Queue<Operation>();
        }

        /// <summary>
        /// Invoke and await his method before you perform a single threaded operation to enqueue it. The method will always return
        /// at the <see cref="IOperationQueueFrameTiming"/> used by this instance.
        /// <br/><br/>
        /// IMPORTANT: it is assumed that this method is always called within the main thread.
        /// </summary>
        public UniTask EnqueueOperationAsync(OperationCost cost = OperationCost.Unknown)
        {
#if DISABLE_OPERATION_QUEUE
            // a way to disable the operation queue both for the editor and runtime through a scripting define
            return UniTask.CompletedTask;
#endif
            
#if UNITY_EDITOR
                // this allows us to test queued vs non-queued code in the editor (use the operation runtime queue inspector)
                if (OperationQueueRuntime.EditorQueuingDisabled)
                    return UniTask.CompletedTask;
#endif
            
            uint operationId = _operationId++;
            var operation = AutoResetUniTaskCompletionSource.Create();
            _operations.Enqueue(new Operation(operationId, cost, operation));
            
            OperationQueueDebugger.DebugEnqueuedOperation(operationId, Timing.Name);
            
            DispatchOperations().Forget();
            return operation.Task;
        }

        private async UniTaskVoid DispatchOperations()
        {
            if (_isDispatching)
            {
                return;
            }

            _isDispatching = true;

            // run across multiple frames until all operations have been dispatched
            while (_operations.Count > 0)
            {
                await Timing.Yield();
                DispatchFrame();
            }
            
            _isDispatching = false;
        }

        // dequeues and executes as many operations as can fit for the current frame
        private void DispatchFrame()
        {
            // peek at the first enqueued operation and check if it can be dispatched
            Operation operation = _operations.Peek();
            if (!OperationQueueRuntime.CanFrameDispatchOperation(_targetDeltaTime, operation.Cost))
            {
                return;
            }

            OperationQueueDebugger.DebugTimingDispatchStart(Timing.Name, _targetDeltaTime);

            do
            {
                DispatchOperation(operation);
                _operations.Dequeue();
                
                if (_operations.Count == 0)
                {
                    return;
                }

                // peek at the next operation
                operation = _operations.Peek();
            }
            while (OperationQueueRuntime.CanFrameDispatchOperation(_targetDeltaTime, operation.Cost));
        }

        private void DispatchOperation(Operation operation)
        {
            OperationQueueDebugger.DebugDispatchingOperation();
            
            // execute operation and measure the time it takes
            float time = Time.realtimeSinceStartup;

            try
            {
                operation.Source.TrySetResult();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(OperationQueue)}] exception thrown by queued operation:\n{exception}");
            }
            
            time = Time.realtimeSinceStartup - time;
            OperationQueueRuntime.DispatchedOperation(time, operation.Cost);
                
            OperationQueueDebugger.DebugDispatchedOperation(operation.Id, time, _targetDeltaTime);
        }

        private readonly struct Operation
        {
            public readonly uint Id;
            public readonly OperationCost Cost;
            public readonly AutoResetUniTaskCompletionSource Source;

            public Operation(uint id, OperationCost cost, AutoResetUniTaskCompletionSource source)
            {
                Id = id;
                Cost = cost;
                Source = source;
            }
        }
    }
}
