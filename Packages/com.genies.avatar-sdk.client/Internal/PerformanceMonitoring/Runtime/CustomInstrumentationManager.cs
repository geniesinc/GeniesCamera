using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;

namespace Genies.PerformanceMonitoring
{
    /// <summary>
    /// Wraps multiple data reporters under one umbrella. Allowing all subscribes reporters to
    /// submit their transactions with one call
    ///
    /// Can treat transactions as singletons or have multiple of the same type using the __Id() functions
    /// </summary>
    public class CustomInstrumentationManager
    {
        private static CustomInstrumentationManager _instance;
        private static List<ICustomInstrumentationHandler> _CurrentHandlers { get; set; } = new List<ICustomInstrumentationHandler>();
        private readonly HashSet<string> _runningTransactions;
        public IReadOnlyCollection<string> RunningTransactions => _runningTransactions;

        //transaction id to transaction id per handler invoked
        private readonly Dictionary<string, List<string>> _runningTransactionsById;

        private readonly Dictionary<string, string> _runningSpans;

        // global tags for transactions
        private Dictionary<string, Func<string>> _globalTags;
        public static bool IsInitialized => _instance != null;

        public static CustomInstrumentationManager Instance
        {
            get
            {
                if (_instance == null)
                {
#if GENIES_INTERNAL
                    CrashReporter.Log("Using default constructor for custom instrumentation manager");
#endif
                    Initialize(null);
                }

                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private CustomInstrumentationManager(Dictionary<string, Func<string>> globalTags)
        {
            _globalTags = globalTags;
            _runningTransactions = new HashSet<string>();
            _runningTransactionsById = new Dictionary<string, List<string>>();
            _runningSpans = new Dictionary<string, string>();
        }

        public static void Initialize(Dictionary<string, Func<string>> globalTags)
        {
            _instance ??= new CustomInstrumentationManager(globalTags);
        }

        public bool AddGlobalTags(string key, Func<string> valFunc)
        {
            if (_globalTags.TryGetValue(key, out _))
            {
                Debug.LogError($"Trying to add a duplicated tag {key} into global tags");
                return false;
            }

            _globalTags.Add(key, valFunc);
            return true;
        }

        public static void AddHandler(ICustomInstrumentationHandler handler)
        {
            foreach (ICustomInstrumentationHandler existingHandler in _CurrentHandlers)
            {
                if (existingHandler.GetType() != handler.GetType())
                {
                    continue;
                }

                //Found handler of this type already
                Debug.LogError($"Trying to add a duplicated handler of type {handler.GetType()}.");
                return;
            }

            Debug.Log($"Adding handler {handler.GetType()} to list of instrumentation handlers");
            _CurrentHandlers.Add(handler);
        }

        private string GetValidSpanGuid()
        {
            var guid = (string)null;
            while (string.IsNullOrEmpty(guid) || _runningSpans.ContainsKey(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            return guid;
        }

        private Dictionary<string, string> GetExtraTags()
        {
            if (_globalTags == null || _globalTags.Count == 0)
            {
                return null;
            }

            var extraTags = new Dictionary<string, string>();
            foreach (KeyValuePair<string, Func<string>> kvp in _globalTags)
            {
                extraTags.Add(kvp.Key, kvp.Value?.Invoke());
            }

            return extraTags;
        }

        /// <summary>
        /// Start a new transaction.
        /// </summary>
        /// <param name="name">transaction name</param>
        /// <param name="operation">operation name</param>
        /// <param name="description">transaction description</param>
        public void StartTransaction(string name, string operation, string description = "")
        {
            if (_runningTransactions.Contains(name))
            {
                Debug.LogError($"Tried to start a transaction that was already running {name}");
                return;
            }

            _runningTransactions.Add(name);

            if (_CurrentHandlers.Count == 0)
            {
                return;
            }

            // set global tags for each transaction
            var extraTags = GetExtraTags();

            _CurrentHandlers.ForEach(
                reporter => reporter?.StartTransaction(
                    name,
                    operation,
                    description,
                    extraTags
                )
            );
        }

        /// <summary>
        /// Finish an existing transaction.
        /// </summary>
        /// <param name="transactionName">transaction name</param>
        public void FinishTransaction(string transactionName)
        {
            _runningTransactions.Remove(transactionName);

            if (_CurrentHandlers.Count == 0)
            {
                return;
            }

            _CurrentHandlers.ForEach(reporter => reporter?.FinishTransaction(transactionName));
        }

        /// <summary>
        /// Finish all running transactions and their child spans.
        /// </summary>
        public void FinishAllTransactions()
        {
            _runningTransactions.Clear();
            _runningTransactionsById.Clear();

            if (_CurrentHandlers.Count == 0)
            {
                return;
            }

            _CurrentHandlers.ForEach(reporter => reporter?.FinishAllTransactions());
        }

        /// <summary>
        /// Start a new child span under an existing root transaction.
        /// </summary>
        /// <param name="transactionName">root transaction name</param>
        /// <param name="spanName">child span name</param>
        /// <param name="spanDescription">child span description</param>
        /// <returns></returns>
        public string StartChildSpanUnderTransaction(string transactionName, string spanName, object spanDescription = null)
        {
            var spanId = GetValidSpanGuid();
            _runningSpans.Add(spanId, spanName);

            if (_CurrentHandlers.Count == 0)
            {
                return spanId;
            }

            _CurrentHandlers.ForEach(
                reporter => reporter?.StartChildSpanUnderTransaction(
                    transactionName,
                    spanName,
                    spanId,
                    spanDescription
                )
            );
            return spanId;
        }

        /// <summary>
        /// Start a new child span under an existing root span.
        /// </summary>
        /// <param name="rootSpanId">root span id</param>
        /// <param name="spanName"></param>
        /// <param name="spanDescription"></param>
        /// <returns></returns>
        public string StartChildSpanUnderSpan(string rootSpanId, string spanName, object spanDescription = null)
        {
            var spanId = GetValidSpanGuid();
            _runningSpans.Add(spanId, spanName);

            if (_CurrentHandlers.Count == 0)
            {
                return spanId;
            }

            _CurrentHandlers.ForEach(
                reporter => reporter?.StartChildSpanUnderSpan(
                    rootSpanId,
                    spanName,
                    spanId,
                    spanDescription
                )
            );

            return spanId;
        }

        /// <summary>
        /// Finish a child span by id.
        /// </summary>
        /// <param name="spanId">child span id</param>
        public void FinishChildSpan(string spanId)
        {
            if (_CurrentHandlers.Count == 0 || string.IsNullOrEmpty(spanId))
            {
                return;
            }

            _CurrentHandlers.ForEach(reporter => reporter?.FinishChildSpan(spanId));
            _runningSpans.Remove(spanId);
        }

        public void FinishChildSpanByName(string spanName)
        {
            if (_CurrentHandlers.Count == 0 || string.IsNullOrEmpty(spanName))
            {
                return;
            }

            var span = _runningSpans.FirstOrDefault(p => p.Value == spanName);

            if (!span.Equals(default(KeyValuePair<string, string>)))
            {
                string spanId = span.Key;
                FinishChildSpan(spanId);
            }
            else
            {
                Debug.LogError($"Tried to finish a span that doesn't exist: {spanName}");
            }
        }

        /// <summary>
        /// Finish all child spans under a root transaction.
        /// </summary>
        /// <param name="rootTransactionName">root transaction name</param>
        public void FinishAllChildSpans(string rootTransactionName)
        {
            if (_CurrentHandlers.Count == 0 || string.IsNullOrEmpty(rootTransactionName))
            {
                return;
            }

            _CurrentHandlers.ForEach(reporter => reporter?.FinishAllChildSpans(rootTransactionName));
            _runningTransactions.Remove(rootTransactionName);
        }

        /// <summary>
        /// Set extra data for an existing transaction.
        /// </summary>
        /// <param name="transactionName">transaction name</param>
        /// <param name="extraKey">key of the extra data</param>
        /// <param name="extraValue">value of the extra data</param>
        public void SetExtraData(string transactionName, string extraKey, string extraValue)
        {
            if (_CurrentHandlers.Count == 0)
            {
                return;
            }

            _CurrentHandlers.ForEach(reporter => reporter?.SetExtraData(transactionName, extraKey, extraValue));
        }

        #region New API

        /// <summary>
        /// Start a new transaction and returns its event id.
        /// </summary>
        /// <param name="name">transaction name</param>
        /// <param name="operation">operation name</param>
        /// <param name="description">transaction description</param>
        public string StartTransactionAndGetId(string name, string operation, string description = "")
        {
            if (_CurrentHandlers.Count == 0)
            {
                return null;
            }

            var extraTags = GetExtraTags();

            string eventId = Guid.NewGuid().ToString();
            _runningTransactionsById[eventId] = new List<string>();
            foreach (var reporter in _CurrentHandlers)
            {
                var subEventId = reporter.StartTransactionAndGetId(name, operation, description, extraTags);
                _runningTransactionsById[eventId].Add(subEventId);
            }

            return eventId;
        }

        /// <summary>
        /// Finish an existing transaction by event id.
        /// </summary>
        /// <param name="eventId">eventId of the transaction</param>
        public void FinishTransactionById(string eventId)
        {
            // check hasn't already finished
            if (!_runningTransactionsById.ContainsKey(eventId))
            {
                return;
            }

            // get sub events and remove
            List<string> subEvents = _runningTransactionsById[eventId];
            _runningTransactionsById.Remove(eventId);

            if (_CurrentHandlers.Count == 0)
            {
                return;
            }

            // finish each subtransaction (bit unoptimized with the double for and sparse collision, but should be fine)
            _CurrentHandlers.ForEach(handler =>
            {
                foreach (var subEvent in subEvents)
                {
                    handler?.FinishTransactionById(subEvent);
                }
            });
        }

        /// <summary>
        /// Start a new child span under an existing root transaction identified by its event id.
        /// </summary>
        /// <param name="eventId">EventId of the root transaction</param>
        /// <param name="spanName">child span name</param>
        /// <param name="spanDescription">child span description</param>
        /// <returns></returns>
        public string StartChildSpanUnderTransactionId(string eventId, string spanName, object spanDescription = null)
        {
            var spanId = GetValidSpanGuid();
            _runningSpans.Add(spanId, spanName);

            if (_CurrentHandlers.Count == 0)
            {
                return spanId;
            }

            List<string> subEvents = _runningTransactionsById[eventId];

            _CurrentHandlers.ForEach(handler =>
            {
                foreach (var subEvent in subEvents)
                {
                    handler?.StartChildSpanUnderTransactionById(
                        subEvent,
                        spanName,
                        spanId,
                        spanDescription
                    );
                }
            });
            return spanId;
        }

        /// <summary>
        /// Finish all child spans under a root transaction identified by an event id.
        /// </summary>
        /// <param name="eventId">EventId of the root transaction</param>
        public void FinishAllChildSpansOfTransactionId(string eventId)
        {
            if (_CurrentHandlers.Count == 0 || string.IsNullOrEmpty(eventId))
            {
                return;
            }

            List<string> subEvents = _runningTransactionsById[eventId];
            _CurrentHandlers.ForEach(handler =>
            {
                foreach (var subEvent in subEvents)
                {
                    handler?.FinishAllChildSpansById(subEvent);
                }
            });
            _runningTransactionsById.Remove(eventId);
        }

        /// <summary>
        /// Set extra data for an existing transaction identified by EventId.
        /// </summary>
        /// <param name="eventId">EventId of the transaction</param>
        /// <param name="extraKey">key of the extra data</param>
        /// <param name="extraValue">value of the extra data</param>
        public void SetExtraDataById(string eventId, string extraKey, string extraValue)
        {
            if (_CurrentHandlers.Count == 0)
            {
                return;
            }

            // get sub events
            List<string> subEventIds = _runningTransactionsById[eventId];

            // Set extra data for each handler
            _CurrentHandlers.ForEach(handler =>
            {
                foreach (var subEventId in subEventIds)
                {
                    handler?.SetExtraDataById(subEventId, extraKey, extraValue);
                }
            });
        }

        #endregion

        #region Utils

        /// <summary>
        /// Measure an async task with a child span.
        /// </summary>
        /// <param name="taskFunc">async task</param>
        /// <param name="transactionName">root transaction name</param>
        /// <param name="spanName">child span name</param>
        /// <param name="description">child span description</param>
        public async UniTask WrapAsyncTaskWithSpan(Func<UniTask> taskFunc, string transactionName, string spanName, object description)
        {
            if (_CurrentHandlers.Count == 0)
            {
                await taskFunc.Invoke();
                return;
            }

            // we must run the operation once while allowing all handlers to wrap it
            var operation = AutoResetUniTaskCompletionSource.Create();
            Func<UniTask> awaitOperationFunc = () => operation.Task;

            IEnumerable<UniTask> handlerTasks = _CurrentHandlers
                .Where(a => a != null)
                .Select(a => a.WrapAsyncTaskWithSpan(awaitOperationFunc, transactionName, spanName, description));

            try
            {
                await taskFunc.Invoke();
                operation.TrySetResult();
            }
            catch (TaskCanceledException exception)
            {
                operation.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                operation.TrySetException(exception);
            }

            await handlerTasks;
        }

        #endregion

        public void Dispose()
        {
            FinishAllTransactions();
            _CurrentHandlers = new List<ICustomInstrumentationHandler>();
            Instance = null;
        }
    }
}
