namespace Genies.Analytics
{
    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string eventName) { }
        public void LogEvent(string eventName, AnalyticProperties properties) { }
        public void SetUserProperty(string propertyName, string value) { }
        public void SetIdentity(IdentityData identityData) { }
        public void IncrementProfileProperty(string key, int incrementValue) { }
    }
}
