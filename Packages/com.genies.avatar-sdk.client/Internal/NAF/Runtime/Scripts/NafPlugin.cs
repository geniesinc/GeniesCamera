using System;
using System.Runtime.InteropServices;
using AOT;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Naf
{
    /**
     * Static representation of the NAF plugin. Containing the state of the plugin and initialization methods.
     */
    public static class NafPlugin
    {
        public static bool IsInitialized { get; private set; }

        private static event Action OnReady;

        private static readonly UniTaskCompletionSource OnReadyCompletionSource = new();

        /**
         * Adds a callback that will be invoked when the NAF plugin is ready to be used (initialized). If the plugin is
         * already initialized, the callback will be invoked immediately.
         */
        public static void AddOnReadyCallback(Action callback)
        {
            if (IsInitialized)
            {
                callback();
            }
            else
            {
                OnReady += callback;
            }
        }

        public static void RemoveOnReadyCallback(Action callback)
        {
            OnReady -= callback;
        }

        /**
         * Returns a UniTask that completes when the NAF plugin is ready to be used (initialized). If the plugin is
         * already initialized, the task will complete immediately.
         */
        public static UniTask AwaitReadyAsync()
        {
            return OnReadyCompletionSource.Task;
        }

        /**
         * Initializes NAF with default settings. See NafSettings.TryLoadDefault()
         */
        public static void Initialize()
        {
            if (NafSettings.TryLoadDefault(out NafSettings settings))
            {
                Initialize(settings);
            }
            else
            {
                Debug.LogError($"Failed to load default {nameof(NafSettings)}.");
            }
        }

        /**
         * Initializes NAF with the given settings. It can only be initialized once.
         */
        public static void Initialize(NafSettings settings)
        {
            if (IsInitialized)
            {
                Debug.LogWarning("NafPlugin is already initialized. You cannot initialize it again.");
                return;
            }

            if (!settings)
            {
                Debug.LogError($"Passed {nameof(NafSettings)} is null or destroyed. Cannot initialize NAF.");
                return;
            }

            // initialize logging (should always go first so any logging performed by the initialization code is captured)
            switch (settings.logging)
            {
                case NafSettings.Logging.Disabled:
                    break;

                case NafSettings.Logging.EditorOnly:
#if UNITY_EDITOR
                    SetLogCallback(LogToUnity);
#endif
                    break;

                case NafSettings.Logging.EditorOrDevBuildOnly:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        SetLogCallback(LogToUnity);
#endif
                    break;

                case NafSettings.Logging.Always:
                    SetLogCallback(LogToUnity);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // special initialization required for Android
#if UNITY_ANDROID && !UNITY_EDITOR
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var packageName = activity.Call<string>("getPackageName");
            SetAndroidPackageName(packageName);
            SetAndroidPaths();
#endif

            // apply the given settings
            settings.Apply();

            // start the render thread loop
            RenderThreadLoop().Forget();

            // finish initialization
            IsInitialized = true;
            OnReady?.Invoke();
            OnReadyCompletionSource.TrySetResult();
        }

        [MonoPInvokeCallback(typeof(LogCallbackDelegate))]
        private static void LogToUnity(string message)
        {
            if (message.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogError(message);
            }
            else if (message.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.LogWarning(message);
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static async UniTaskVoid RenderThreadLoop()
        {
            // for now, Metal is our only known supported Graphics API that does not require render thread synchronization
            if (SystemInfo.graphicsDeviceType is GraphicsDeviceType.Metal)
            {
                return;
            }

            IntPtr renderEventFuncPtr = GetRenderEventFunc();
            if (renderEventFuncPtr == IntPtr.Zero)
            {
                Debug.LogError("NAF initialization error: failed to get render event function pointer from native plugin.");
                return;
            }

            // issue the render event on every frame
            while (Application.isPlaying)
            {
                await UniTask.Yield();
                GL.IssuePluginEvent(renderEventFuncPtr, 1);
            }
        }

#region Native Functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LogCallbackDelegate(string message);

        [DllImport(Genies.NafPlugin.ImportName.Value)]
        private static extern void SetLogCallback(LogCallbackDelegate callback);

#if UNITY_ANDROID && !UNITY_EDITOR
        [DllImport(Genies.NafPlugin.ImportName.Value)]
        private static extern void SetAndroidPackageName(string packageName);

        [DllImport(Genies.NafPlugin.ImportName.Value)]
        private static extern void SetAndroidPaths();
#endif

        [DllImport(Genies.NafPlugin.ImportName.Value)]
        private static extern IntPtr GetRenderEventFunc();
#endregion
    }
}
