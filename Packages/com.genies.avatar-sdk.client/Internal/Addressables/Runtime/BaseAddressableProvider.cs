namespace Genies.Addressables
{
    public class BaseAddressableProvider
    {
        private const string DynamicContentUrl = "https://d3vwr5y0neqoqu.cloudfront.net";

        public static string DynBaseUrl
        {
            get
            {
                return DynamicContentUrl;
            }
        }

    }
}
