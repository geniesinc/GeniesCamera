namespace Genies.CrashReporting
{
    /// <summary>
    /// Defines the severity levels for logging operations in the crash reporting system.
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// Standard informational log message.
        /// </summary>
        Log,
        
        /// <summary>
        /// Warning message indicating a potential issue.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error message indicating a significant problem.
        /// </summary>
        Error,
        
        /// <summary>
        /// Exception message indicating a critical failure.
        /// </summary>
        Exception
    }
}
