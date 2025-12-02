using UnityEngine;

namespace Genies.Addressables
{
    public abstract class BaseAddressablesService
    {
        public static string GetPlatformString()
        {
            return Application.platform switch
            {
                RuntimePlatform.IPhonePlayer => "iOS",
                RuntimePlatform.Android => "Android",
                RuntimePlatform.WindowsEditor => "StandaloneWindows64",
                RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
                RuntimePlatform.OSXEditor => "StandaloneOSX",
                RuntimePlatform.OSXPlayer => "StandaloneOSX",
                RuntimePlatform.LinuxEditor => "StandaloneLinux64",
                RuntimePlatform.LinuxPlayer => "StandaloneLinux64",
                RuntimePlatform.WebGLPlayer => "WebGL",
                _ => string.Empty,
            };
        }
    }
}
