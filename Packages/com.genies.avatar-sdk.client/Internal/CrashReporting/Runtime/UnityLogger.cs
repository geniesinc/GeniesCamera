using System;
using System.Collections.Generic;

#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
using Genies.CrashReporting.Helpers;
#endif

using UnityEngine;
using static UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Genies.CrashReporting
{
    /// <summary>
    /// Unity-based implementation of <see cref="ICrashLogger"/> that routes crash reports and logs through Unity's logging system.
    /// This logger integrates with Unity's built-in logging infrastructure and formats messages with enhanced stack trace information.
    /// </summary>
    public class UnityLogger : ICrashLogger
    {
        private class UnityLogHandler : ILogHandler
        {
            private readonly ILogHandler _originalHandler;

            public UnityLogHandler(ILogHandler originalHandler)
            {
                _originalHandler = originalHandler;
            }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {

#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
                var message = format;

                if (!message.Contains(ReportingHelpers.CleanStackTraceTag))
                {
                    message = ReportingHelpers.FormatMessageWithStackTrace(format);
                }

                _originalHandler.LogFormat(logType, context, message, args);
#endif
            }

            public void LogException(Exception exception, Object context)
            {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS

                var message = exception.ToString();

                if (!message.Contains(ReportingHelpers.CleanStackTraceTag))
                {
                    message = ReportingHelpers.FormatMessageWithStackTrace("");
                    _originalHandler.LogException(new Exception(message, exception), context);
                }
                else
                {
                    _originalHandler.LogException(exception, context);
                }

#endif
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityLogger"/> class.
        /// Replaces Unity's default log handler with a custom handler that enhances stack trace formatting.
        /// </summary>
        public UnityLogger()
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            var originalHandler = unityLogger.logHandler;
            unityLogger.logHandler = new UnityLogHandler(originalHandler);
#endif
        }

        [HideInCallstack]
        public void LogHandledException(Exception exception)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            unityLogger.LogException(exception);
#endif
        }

        [HideInCallstack]
        public void LogHandledException(Exception exception, Dictionary<string, object> extraData)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            foreach (var datum in extraData)
            {
                exception.Data.Add(datum.Key, datum.Value);
            }

            unityLogger.LogException(exception);
#endif
        }

        [HideInCallstack]
        public void LeaveBreadcrumb(string breadcrumb)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            var formattedMessage = ReportingHelpers.FormatMessage(breadcrumb, LogSeverity.Log);
            Log(formattedMessage);
#endif
        }

        [HideInCallstack]
        public void SetUserId(string id)
        {
#if UNITY_EDITOR || QA_BUILD
            Log($"[Crash Reporter] UserId set: {id}");
#endif
        }

        [HideInCallstack]
        public void Log(string message, LogSeverity logSeverity = LogSeverity.Log)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ReportingHelpers.DebugLog(message, logSeverity);
#endif
        }

        [HideInCallstack]
        public void SetCustomData(string key, string value)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            Log($"[Crash Reporter] custom data set {key} : {value}");
#endif
        }
    }
}
