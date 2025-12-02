using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.PerformanceMonitoring
{
    public interface ICustomInstrumentationHandler
    {
        /// <summary>
        /// Start a new transaction.
        /// </summary>
        /// <param name="name">transaction name</param>
        /// <param name="operation">operation name</param>
        /// <param name="description">transaction description</param>
        /// <param name="extraTags">global tag(s) for this transaction</param>
        public void StartTransaction(string name, string operation, string description = "", Dictionary<string, string> extraTags = null);

        /// <summary>
        /// Finish an existing transaction.
        /// </summary>
        /// <param name="transactionName">transaction name</param>
        public void FinishTransaction(string transactionName);

        /// <summary>
        /// Finish all running transactions and their child spans.
        /// </summary>
        public void FinishAllTransactions();

        /// <summary>
        /// Start a new child span under an existing root transaction.
        /// </summary>
        /// <param name="transactionName">root transaction name</param>
        /// <param name="spanName">child span name</param>
        /// <param name="spanId">child span id</param>
        /// <param name="spanDescription">child span description</param>
        public void StartChildSpanUnderTransaction(string transactionName, string spanName, string spanId, object spanDescription = null);

        /// <summary>
        /// Start a new child span under an existing root span.
        /// </summary>
        /// <param name="rootSpanId">root span id</param>
        /// <param name="spanName">child span name</param>
        /// <param name="spanId">child span id</param>
        /// <param name="spanDescription">child span description</param>
        public void StartChildSpanUnderSpan(string rootSpanId, string spanName, string spanId, object spanDescription = null);

        /// <summary>
        /// Finish a child span by id.
        /// </summary>
        /// <param name="spanId">child span id</param>
        public void FinishChildSpan(string spanId);

        /// <summary>
        /// Finish all child spans under a root transaction.
        /// </summary>
        /// <param name="rootTransactionName">root transaction name</param>
        public void FinishAllChildSpans(string rootTransactionName);

        /// <summary>
        /// Set extra data for an existing transaction.
        /// </summary>
        /// <param name="transactionName">transaction name</param>
        /// <param name="extraKey">key of the extra data</param>
        /// <param name="extraValue">value of the extra data</param>
        public void SetExtraData(string transactionName, string extraKey, string extraValue);

        /// <summary>
        /// Measure an async task with a child span.
        /// </summary>
        /// <param name="taskFunc">async task</param>
        /// <param name="transactionName">root transaction name</param>
        /// <param name="spanName">child span name</param>
        /// <param name="description">child span description</param>
        /// <returns></returns>
        public UniTask WrapAsyncTaskWithSpan(Func<UniTask> taskFunc, string transactionName, string spanName, object description);

        /// <summary>
        /// Start a new transaction and return its unique identifier.
        /// </summary>
        /// <param name="name">Transaction name.</param>
        /// <param name="operation">Operation name for the transaction.</param>
        /// <param name="description">Optional description of the transaction.</param>
        /// <param name="extraTags">Optional global tag(s) for this transaction.</param>
        /// <returns>The unique identifier of the started transaction.</returns>
        public string StartTransactionAndGetId(string name, string operation, string description = "", Dictionary<string, string> extraTags = null);

        /// <summary>
        /// Finish an existing transaction by its unique identifier.
        /// </summary>
        /// <param name="eventId">The unique identifier of the transaction to finish.</param>
        public void FinishTransactionById(string eventId);

        /// <summary>
        /// Start a new child span under an existing transaction by its unique identifier.
        /// </summary>
        /// <param name="eventId">The unique identifier of the root transaction.</param>
        /// <param name="spanName">The name of the child span.</param>
        /// <param name="spanId">The unique identifier for the child span.</param>
        /// <param name="spanDescription">Optional description of the child span.</param>
        public void StartChildSpanUnderTransactionById(string eventId, string spanName, string spanId, object spanDescription = null);

        /// <summary>
        /// Finish all child spans under a specified transaction by its unique identifier.
        /// </summary>
        /// <param name="eventId">The unique identifier of the root transaction.</param>
        public void FinishAllChildSpansById(string eventId);

        /// <summary>
        /// Set extra data for an existing transaction by its unique identifier.
        /// </summary>
        /// <param name="eventId">The unique identifier of the transaction.</param>
        /// <param name="extraKey">The key of the extra data.</param>
        /// <param name="extraValue">The value of the extra data.</param>
        public void SetExtraDataById(string eventId, string extraKey, string extraValue);

    }
}
