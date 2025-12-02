namespace Genies.Assets.Services
{
    /// <summary>
    /// Constants with the LOD strings available for assets.
    /// </summary>
    public static class AssetLod
    {
        public const string Default = Medium;
        public const string Render = High;
        public const string Low = "lod2";
        public const string Medium = "lod1";
        public const string High = "lod0";

        private const string _lodAddressFormat = "{0}_{1}";

        public static object InterpolatedAddress(object key, string lod)
        {
            return string.Format(_lodAddressFormat, key, lod);
        }
    }
}
