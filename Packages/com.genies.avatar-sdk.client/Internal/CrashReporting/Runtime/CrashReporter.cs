using System;
using System.Collections.Generic;
using Genies.CrashReporting.Helpers;
using UnityEngine;

namespace Genies.CrashReporting
{
    /// <summary>
    /// Reports exceptions/breadcrumbs to a list of <see cref="ICrashLogger"/>
    /// </summary>
    public static class CrashReporter
    {
        private static List<ICrashLogger> CurrentLoggers { get; } = new List<ICrashLogger>();


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            AddLogger(new UnityLogger());
#endif
        }

        /// <summary>
        /// Adds a crash logger to the list of active loggers.
        /// Prevents duplicate loggers of the same type from being added.
        /// </summary>
        /// <param name="logger">The crash logger implementation to add.</param>
        public static void AddLogger(ICrashLogger logger)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            foreach (ICrashLogger existingReporter in CurrentLoggers)
            {
                if (existingReporter.GetType() != logger.GetType())
                {
                    continue;
                }

                //Found reporter of this type already
                LogAction($"Trying to add a duplicated reporter of type {logger.GetType()}.", LogSeverity.Warning);
                return;
            }

            LogActionInternal($"Adding reporter {logger.GetType()} to list of crash reporters", LogSeverity.Log);
            CurrentLoggers.Add(logger);
#endif
        }

        [HideInCallstack]
        private static void LogAction(string message, LogSeverity severity)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            const string tag              = "[CRASH REPORTER]";
            var          formattedMessage = $"{tag} {message}";

            ReportingHelpers.DebugLog(formattedMessage, severity);
#endif
        }

        [HideInCallstack]
        private static void LogActionInternal(string message, LogSeverity severity)
        {
#if GENIES_INTERNAL || QA_BUILD || FORCE_LOGS
            const string tag              = "[CRASH REPORTER]";
            var          formattedMessage = $"{tag} {message}";

            ReportingHelpers.DebugLog(formattedMessage, severity);
#endif
        }

        /// <summary>
        /// Logs the passed exception to the associated reporter.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        [HideInCallstack]
        public static void LogHandledException(Exception exception)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.LogHandledException(exception));
#endif
        }

        /// <summary>
        /// Logs the passed exception with associated tags to the associated reporter.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="extraData">The tags to add to the exception.</param>
        [HideInCallstack]
        public static void LogHandledException(Exception exception, Dictionary<string, object> extraData)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.LogHandledException(exception, extraData));
#endif
        }

        /// <summary>
        /// Logs a breadcrumb with the reporter that will be
        /// submitted alongside any events or errors.
        /// </summary>
        /// <param name="breadcrumb">The breadcrumb to log.</param>
        [HideInCallstack]
        public static void LeaveBreadcrumb(object breadcrumb)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.LeaveBreadcrumb(breadcrumb.ToString()));
#endif
        }

        /// <summary>
        /// Logs a simple message with severity
        /// </summary>
        /// <param name="message"> The message to log </param>
        /// <param name="severity"> The severity of the message </param>
        [HideInCallstack]
        public static void Log(object message, LogSeverity severity = LogSeverity.Log)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.Log(message.ToString(), severity));
#endif
        }

        /// <summary>
        /// Logs a simple message with severity if an internal scripting define is enabled
        /// </summary>
        /// <param name="message"> The message to log </param>
        /// <param name="severity"> The severity of the message </param>
        [HideInCallstack]
        public static void LogInternal(object message, LogSeverity severity = LogSeverity.Log)
        {
#if GENIES_INTERNAL || QA_BUILD|| FORCE_LOGS
            ForeachLogger(reporter => reporter?.Log(message.ToString(), severity));
#endif
        }

        /// <summary>
        /// Logs a simple message with error severity
        /// </summary>
        /// <param name="message"> The message to log </param>
        [HideInCallstack]
        public static void LogWarning(object message)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.Log(message.ToString(), LogSeverity.Warning));
#endif
        }

        /// <summary>
        /// Logs a simple message with error severity
        /// </summary>
        /// <param name="message"> The message to log </param>
        [HideInCallstack]
        public static void LogError(object message)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.Log(message.ToString(), LogSeverity.Error));
#endif
        }

        /// <summary>
        /// Sets the current end-user associated with the reporter.
        /// </summary>
        /// <param name="id">The user's ID.</param>
        [HideInCallstack]
        public static void SetUserId(string id)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.SetUserId(id));
#endif
        }

        /// <summary>
        /// Sets custom data to be associated with crashes
        /// </summary>
        /// <param name="key"> Data key </param>
        /// <param name="value"> Data </param>
        [HideInCallstack]
        public static void SetCustomData(string key, string value)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            ForeachLogger(reporter => reporter?.SetCustomData(key, value));
#endif
        }

        [HideInCallstack]
        private static void ForeachLogger(Action<ICrashLogger> action)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            foreach (var logger in CurrentLoggers)
            {
                if (logger == null)
                {
                    continue;
                }

                action?.Invoke(logger);
            }
#endif
        }
    }
}
