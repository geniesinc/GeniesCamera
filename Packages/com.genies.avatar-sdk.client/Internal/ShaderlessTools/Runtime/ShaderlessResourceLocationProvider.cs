namespace Genies.Components.ShaderlessTools
{
    public static class ShaderlessResourceLocationProvider
    {
        public static string PrimaryKey(string hash) => $"{hash}";
        public static string InternalId(string hash) => $"Assets/Genies_Content/Shaders/{hash}/{hash}.asset";
        public static string BundleKey(string hash, int version) => $"dynamicgroupshaders_assets_internal_{hash}_v{version}.bundle";
        public static string RemoteUrl(string hash, string platform, string dynBaseUrl, int version) =>
            $"{dynBaseUrl}/internal/Shaders/{hash}/v{version}/{platform}/{BundleKey(hash, version)}";
    }
}
