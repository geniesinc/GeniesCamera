using System;
using Genies.CrashReporting;
using UnityEngine;

namespace Genies.Utilities
{
    public static class Validation
    {
        public static bool IsValid(object inObject, string inErrorMessage = null)
        {
            if (inObject != null)
            {
                return true;
            }

            ReportError(inErrorMessage);
            return false;
        }

        public static bool IsValid(object inObject, Exception inLogException = null)
        {
            if (inObject != null)
            {
                return true;
            }

            ReportError(inLogException);
            return false;

        }

        public static bool IsValid(string inString, string inErrorMessage = null)
        {
            if (!string.IsNullOrEmpty(inString))
            {
                return true;
            }

            ReportError(inErrorMessage);
            return false;
        }

        public static bool IsValid(string inString, Exception inLogException = null)
        {
            if (!string.IsNullOrEmpty(inString))
            {
                return true;
            }

            ReportError(inLogException);
            return false;
        }

        public static void ReportError(Exception inLogException)
        {
            // Register the exception in the unity diagnostic dashboard
            if(inLogException != null)
            {
                CrashReporter.LogHandledException(inLogException);
            }
        }

        public static void ReportError(string inErrorMessage)
        {
            // Register the exception in the unity diagnostic dashboard
            if(!string.IsNullOrEmpty(inErrorMessage))
            {
                CrashReporter.LogHandledException(new Exception(inErrorMessage));
            }
        }
    }
}
