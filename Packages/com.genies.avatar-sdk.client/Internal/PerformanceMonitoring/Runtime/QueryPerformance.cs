using System;
using Genies.CrashReporting;

namespace Genies.PerformanceMonitoring
{
    public class QueryPerformance
    {
        private static CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;

        /// <summary>
        /// Starts a transaction that will be used to monitor the performance of a query. It follows the proper naming
        /// conventions based on the Sentry Guide. https://docs.sentry.io/product/performance/queries/
        /// </summary>
        /// <param name="transactionName">Name of the transaction.</param>
        /// <param name="query">Query to measure</param>
        public static void StartQueryTransaction(string transactionName, string query)
        {
            try
            {
                _InstrumentationManager.StartTransaction(transactionName, "db.query");
                _InstrumentationManager.StartChildSpanUnderTransaction(transactionName, "db.sql.execute", query);
            }
            catch (Exception e)
            {
                CrashReporter.Log($"Error starting query transaction: {e.Message}", LogSeverity.Exception);
            }
        }

        /// <summary>
        /// Ends a transaction that will be used to monitor the performance of a query. It follows the proper naming
        /// conventions based on the Sentry Guide. https://docs.sentry.io/product/performance/queries/
        /// </summary>
        /// <param name="transactionName">Name of the transaction.</param>
        public static void CompleteQueryTransaction(string transactionName)
        {
            try
            {
                _InstrumentationManager.FinishAllChildSpans(transactionName);
                _InstrumentationManager.FinishTransaction(transactionName);
            }
            catch (Exception e)
            {
                CrashReporter.Log($"Error completing query transaction: {e.Message}", LogSeverity.Exception);
            }
        }
    }
}
