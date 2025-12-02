using Genies.Sdk.Bootstrap.Editor;
using UnityEditor;

namespace Genies.SdkBootstrap.Editor
{
    internal static class MenuItems
    {
        private static ExternalLinks ExternalLinksInstance { get; } = new ();

        public static class ExternaLinks
        {
            [MenuItem("Tools/Genies/Genies Hub", priority = -100)]
            public static void OpenGeniesHub()
            {
                ExternalLinksInstance.OpenGeniesHub();
            }

            [MenuItem("Tools/Genies/Support/Technical Documentation", priority = -99)]
            public static void OpenGeniesTechnicalDocumentation()
            {
                ExternalLinksInstance.OpenGeniesTechnicalDocumentation();
            }

            [MenuItem("Tools/Genies/Support/Genies Support", priority = -98)]
            public static void OpenGeniesSupport()
            {
                ExternalLinksInstance.OpenGeniesSupport();
            }
        }
    }
}

