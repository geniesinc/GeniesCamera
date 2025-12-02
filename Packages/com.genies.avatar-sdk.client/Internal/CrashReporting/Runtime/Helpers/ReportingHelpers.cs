using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using static UnityEngine.Debug;

namespace Genies.CrashReporting.Helpers
{
    /// <summary>
    /// Utility class providing helper methods for formatting log messages and handling stack traces in crash reporting.
    /// This class contains methods for enhancing log output with clean stack trace information and appropriate message formatting.
    /// </summary>
    public static class ReportingHelpers
    {
        private const string _logTag = "[Breadcrumb]";
        private const string _errorTag = "[Error]";
        private const string _warningTag = "[Warning]";
        private const string _exceptionTag = "[Exception]";

        internal const string CleanStackTraceTag = "Clean Trace:";

        private static readonly List<string> _ignoredFiles = new List<string>()
        {
            $"{nameof(CrashReporter)}.cs",
            $"{nameof(UnityLogger)}.cs",
            $"{nameof(ReportingHelpers)}.cs",
            "Logging.cs",
            "Logger.cs",
            "Debug.bindings.cs",
            "UnityLogHandlerIntegration.cs",
        };

        /// <summary>
        /// Formats a message with the appropriate severity tag for consistent logging output.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="severity">The severity level to determine the appropriate tag.</param>
        /// <returns>A formatted message string with the severity-specific tag prefix.</returns>
        [HideInCallstack]
        public static string FormatMessage(string message, LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Log:
                    return $"{_logTag} {message}";
                case LogSeverity.Warning:
                    return $"{_warningTag} {message}";
                case LogSeverity.Error:
                    return $"{_errorTag} {message}";
                case LogSeverity.Exception:
                    return $"{_exceptionTag} {message}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        }

        /// <summary>
        /// Formats a message with enhanced stack trace information for debugging purposes.
        /// </summary>
        /// <param name="message">The message to format with stack trace information.</param>
        /// <returns>A formatted message string that includes clean stack trace details.</returns>
        public static string FormatMessageWithStackTrace(string message)
        {
            return CleanStackTraceMessageFormat(message);
        }

        [HideInCallstack]
        private static string CleanStackTraceMessageFormat(string message)
        {
            var stackTrace     = new StackTrace(skipFrames: 1, fNeedFileInfo: true);
            var stackTraceText = stackTrace.GetCleanStackTraceWithFileLinks(ignoreFramesWithoutFileInfo: true, ignoredFileNames: _ignoredFiles);

            return $"{message}\n{CleanStackTraceTag} {stackTraceText}";
        }


        /// <summary>
        /// Logs a message to Unity's debug console with enhanced stack trace information.
        /// The message is automatically formatted with clean stack trace details for better debugging.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="logSeverity">The severity level of the message (default: Log).</param>
        [HideInCallstack]
        public static void DebugLog(string message, LogSeverity logSeverity = LogSeverity.Log)
        {
#if UNITY_EDITOR || QA_BUILD || FORCE_LOGS
            var fullMessage = CleanStackTraceMessageFormat(message);

            switch (logSeverity)
            {
                case LogSeverity.Log:
                    Log(fullMessage);
                    break;
                case LogSeverity.Warning:
                    LogWarning(fullMessage);
                    break;
                case LogSeverity.Error:
                    LogError(fullMessage);
                    break;
                case LogSeverity.Exception:
                    unityLogger.LogException(new Exception(fullMessage));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logSeverity), logSeverity, null);
            }
#endif
        }
    }
}
