namespace Genies.Addressables
{
    public enum AddressablesCatalogContentType
    {
        Static,
        Generative,
        Looks,
        Library,
        Dynamic,
        DynamicExternal,
    }

    public static class AddressableCatalogContentTypeExtensions
    {
        public static string ToLowercaseString(this AddressablesCatalogContentType enumValue)
        {
            return enumValue.ToString().ToLower();
        }
    }
}
