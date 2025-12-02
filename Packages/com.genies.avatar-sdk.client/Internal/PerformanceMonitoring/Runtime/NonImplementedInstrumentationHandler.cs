using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.PerformanceMonitoring
{
    public class NonImplementedInstrumentationHandler : ICustomInstrumentationHandler
    {
        public void StartTransaction(string name, string operation, string description = "", Dictionary<string, string> extraTags = null) { }

        public void FinishTransaction(string transactionName) { }

        public void FinishAllTransactions() { }

        public void StartChildSpanUnderTransaction(string transactionName, string spanName,string spanId, object spanDescription = null) { }

        public void StartChildSpanUnderSpan(string rootSpanId, string spanName, string spanId, object spanDescription = null) { }

        public void FinishChildSpan(string spanId) { }

        public void FinishAllChildSpans(string rootTransactionName) { }

        public void SetExtraData(string transactionName, string extraKey, string extraValue) { }

        public UniTask WrapAsyncTaskWithSpan(Func<UniTask> taskFunc, string transactionName, string spanName, object description)
        {
            return taskFunc.Invoke();
        }

        public string StartTransactionAndGetId(string name, string operation, string description = "", Dictionary<string, string> extraTags = null)
        {
            throw new NotImplementedException();
        }

        public void FinishTransactionById(string eventId)
        {
            throw new NotImplementedException();
        }

        public void StartChildSpanUnderTransactionById(string eventId, string spanName, string spanId, object spanDescription = null)
        {
            throw new NotImplementedException();
        }

        public void FinishAllChildSpansById(string eventId)
        {
            throw new NotImplementedException();
        }

        public void SetExtraDataById(string eventId, string extraKey, string extraValue)
        {
            throw new NotImplementedException();
        }
    }
}
