using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Genies.CrashReporting.Helpers
{
    public static class StackTraceExtensions
    {
        private static readonly StringBuilder _builder = new StringBuilder();


        /// <summary>
        /// Returns a clean and easy to read stack trace for the Unity console with the file links.
        /// </summary>
        [HideInCallstack]
        public static string GetCleanStackTraceWithFileLinks(
            this StackTrace stackTrace,
            bool ignoreFramesWithoutFileInfo = false,
            List<string> ignoredFileNames = null)
        {
            _builder.Clear();

            for (var i = 0; i < stackTrace.FrameCount; ++i)
            {
                var frame    = stackTrace.GetFrame(i);
                var filePath = frame.GetFileName();

                if (ignoreFramesWithoutFileInfo && string.IsNullOrEmpty(filePath))
                {
                    continue;
                }

                var fileName = Path.GetFileName(filePath);

                if (ignoredFileNames != null && ignoredFileNames.Contains(fileName))
                {
                    continue;
                }

                var line       = frame.GetFileLineNumber();
                var methodName = frame.GetMethod().Name;

                _builder.AppendLine($"<a href=\"{filePath}\" line=\"{line}\">{fileName}:{line}</a> in {methodName}()");
            }

            return _builder.ToString();
        }
    }
}
