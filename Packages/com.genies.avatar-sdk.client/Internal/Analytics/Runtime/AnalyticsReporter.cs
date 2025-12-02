using System;
using System.Collections.Generic;
using Genies.CrashReporting;

namespace Genies.Analytics
{
    public static class AnalyticsReporter
    {
        private static List<IAnalyticsService> _CurrentReporters { get; } = new List<IAnalyticsService>();

        public static void AddReporter(IAnalyticsService reporter)
        {
            foreach (IAnalyticsService existingReporter in _CurrentReporters)
            {
                if (existingReporter.GetType() != reporter.GetType())
                {
                    continue;
                }

                //Found reporter of this type already
                CrashReporter.LogWarning($"Trying to add a duplicated reporter of type {reporter.GetType()}.");
                return;
            }


            CrashReporter.Log($"Adding reporter {reporter.GetType()} to list of analytics reporters");
            _CurrentReporters.Add(reporter);
        }

        public static void LogEvent(string eventName)
        {
            ForeachLogger(reporter => reporter?.LogEvent(eventName));
        }

        public static void LogEvent(string eventName, AnalyticProperties properties)
        {
            ForeachLogger(reporter => reporter?.LogEvent(eventName, properties));
        }

        public static void SetUserProperty(string propertyName, string value)
        {
            ForeachLogger(reporter => reporter?.SetUserProperty(propertyName, value));
        }

        public static void SetIdentity(IdentityData identityData)
        {
            ForeachLogger(reporter => reporter?.SetIdentity(identityData));
        }

        public static void IncrementProfileProperty(string key, int incrementValue)
        {
            ForeachLogger(reporter => reporter?.IncrementProfileProperty(key, incrementValue));
        }

        private static void ForeachLogger(Action<IAnalyticsService> action)
        {
            foreach (var logger in _CurrentReporters)
            {
                if (logger == null)
                {
                    continue;
                }

                action?.Invoke(logger);
            }
        }
    }
}
