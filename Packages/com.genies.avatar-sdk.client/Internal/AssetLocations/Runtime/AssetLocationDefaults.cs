namespace Genies.AssetLocations
{
    public static class AssetLocationDefaults
    {
        // need to keep in sync with AssetService..
        // was not included to avoid cyclical deps.. since its just a string.
        public static readonly string[] AssetLods = { "", $"_lod1", $"_lod2", };
        public static readonly string[] IconSizes = { "", $"_x512", $"_x1024", };

    }
}
