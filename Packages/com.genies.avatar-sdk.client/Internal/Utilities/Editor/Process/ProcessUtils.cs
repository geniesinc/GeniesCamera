using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Genies.Utilities.Editor
{
    /// <summary>
    /// Small util for handling processes outside of unity.
    /// </summary>
    public class ProcessUtils
    {
        private static readonly Dictionary<string, string> CommandCache = new Dictionary<string, string>();

        public static string GetExecutableFilePath(string command, string directory)
        {
            return GetExecutableFilePath(command, new[] { directory }, true);
        }

        public static string GetExecutableFilePath(string command,
                                                   string[] searchPaths,
                                                   bool ignoreCache = false
        )
        {
            if (!ignoreCache && CommandCache.TryGetValue(command, out var path))
            {
                return path;
            }

            string[] extensions;
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                extensions = new[] { "" };
            }
            else
            {
                extensions = new[] { ".cmd", ".exe" };
            }

            foreach (var searchPath in searchPaths)
            {
                foreach (var str in extensions)
                {
                    path = Path.Combine(searchPath, command + str);
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    CommandCache[command] = path;
                    return path;
                }
            }

            return string.Empty;
        }

        public static async Task<ProcessResult> StartProcessAsync(string fileName, string arguments, string workingDirectory, int timeout = 30000)
        {
            var tcs = new TaskCompletionSource<ProcessResult>();
            var process = new Process();
            var result = new ProcessResult();
            process.StartInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    result.Outputs.Add(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    result.Errors.Add(e.Data);
                }
            };

            process.Exited += (sender, e) =>
            {
                tcs.TrySetResult(result);
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var awaited = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

            // Timeout handling
            if (awaited == tcs.Task)
            {
                return await tcs.Task;
            }

            process.Kill();
            result.Outputs = null;
            result.Errors.Add("Process Timed Out");
            return result;
        }

        public static ProcessResult StartProcess(string fileName,
                                                 string workingDirectory,
                                                 StringDictionary envVariables = null,
                                                 int timeout = 30000,
                                                 params string[] arguments
        )
        {

            var startInfo = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            foreach (var arg in arguments)
            {
                startInfo.ArgumentList.Add(arg);
            }
            
            var process = new Process
            {
                StartInfo = startInfo
            };

            // If additional environment variables are provided, set them
            if (envVariables != null)
            {
                foreach (string key in envVariables.Keys)
                {
                    process.StartInfo.EnvironmentVariables[key] = envVariables[key];
                }
            }

            var result = new ProcessResult();
            var outputWaitHandle = new AutoResetEvent(false);
            var errorWaitHandle = new AutoResetEvent(false);

            try
            {
                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        result.Outputs.Add(e.Data);
                    }
                };
                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        result.Errors.Add(e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (process.WaitForExit(timeout) &&
                    outputWaitHandle.WaitOne(timeout) &&
                    errorWaitHandle.WaitOne(timeout))
                {
                    return result;
                }

                process.Kill();
                result.Outputs = null;
                result.Errors.Add("Process Timed Out");
                return result;
            }
            finally
            {
                outputWaitHandle.Dispose();
                errorWaitHandle.Dispose();
                process.Dispose();
            }
        }
        
        private static string AddQuotesForWhiteSpace(string path, bool isWindows)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (isWindows)
            {
                // On Windows, add double quotes if the path contains spaces
                if (path.Contains(" ") && !path.StartsWith("\"") && !path.EndsWith("\""))
                {
                    return "\"" + path + "\"";
                }
                else
                {
                    return path;
                }
            }
            else
            {
                // On Unix-like systems, wrap the path in single quotes if it contains spaces
                if (path.Contains(" ") && !path.StartsWith("'") && !path.EndsWith("'"))
                {
                    return "'" + path.Replace("'", "'\\''") + "'";
                }
                else
                {
                    return path;
                }
            }
        }


        private static string QuoteArguments(string arguments)
        {
            var parts = arguments.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Contains(" "))
                {
                    parts[i] = $"\"{parts[i]}\"";
                }
            }

            return string.Join(" ", parts);
        }
    }

    public class ProcessResult
    {
        public List<string> Errors = new List<string>();
        public List<string> Outputs = new List<string>();
        public string LastOutput => Outputs is { Count: > 0 } ? Outputs[^1] : null;
        public string LastError => Errors is { Count: > 0 } ? Errors[^1] : null;
        public string FlattenedErrors => Errors is { Count: > 0 } ? string.Join("\n", Errors) : null;
        public bool WasSuccessful => Errors == null || Errors.Count == 0;
    }
}
