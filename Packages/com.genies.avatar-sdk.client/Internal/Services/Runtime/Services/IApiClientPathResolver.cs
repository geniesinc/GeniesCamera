namespace Genies.Services.Configs
{
    public interface IApiClientPathResolver
    {
        string GetApiBaseUrl(BackendEnvironment environment);
    }
}
