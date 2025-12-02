using UnityEngine;

namespace Genies.Sdk.Bootstrap.Editor
{
    public class ExternalLinks
    {
        public void OpenGeniesHub()
        {
            Application.OpenURL("https://hub.genies.com/");
        }

        public void OpenGeniesTechnicalDocumentation()
        {
            Application.OpenURL("https://docs.genies.com/docs/intro");
        }

        public void OpenGeniesSupport()
        {
            Application.OpenURL("https://support.genies.com/hc/en-us");
        }
    }
}
