namespace Genies.Addressables
{
    public class DummyContentOverrideService : IContentOverrideService
    {
        public string GetOverrideUrl(string fallback)
        {
            return fallback;
        }
    }
}
