using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.CrashReporting
{
    public interface ICrashLogger
    {
        /// <summary>
        /// Logs the passed exception to the associated reporter.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        [HideInCallstack]
        void LogHandledException( Exception exception );

        /// <summary>
        /// Logs the passed exception with associated tags to the associated reporter.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="extraData">The tags to add to the exception.</param>
        [HideInCallstack]
        void LogHandledException( Exception exception, Dictionary<string, object> extraData );

        /// <summary>
        /// Logs a breadcrumb with the reporter that will be
        /// submitted alongside any events or errors.
        /// </summary>
        /// <param name="breadcrumb">The breadcrumb to log.</param>
        [HideInCallstack]
        void LeaveBreadcrumb( string breadcrumb );

        /// <summary>
        /// Sets the current end-user associated with the reporter.
        /// </summary>
        /// <param name="id">The user's ID.</param>
        [HideInCallstack]
        void SetUserId (string id);

        /// <summary>
        /// Logs a simple message with severity
        /// </summary>
        /// <param name="message"> The message to log </param>
        /// <param name="logSeverity"> The severity of the message </param>
        [HideInCallstack]
        void Log(string message, LogSeverity logSeverity = LogSeverity.Log);

        /// <summary>
        /// Sets custom data to be associated with crashes
        /// </summary>
        /// <param name="key"> Data key </param>
        /// <param name="value"> Data </param>
        [HideInCallstack]
        void SetCustomData(string key, string value);
    }
}
