using Newtonsoft.Json.Linq;

namespace Genies.Components.ShaderlessTools
{
    public static class JsonObjectUtility
    {
        /// <summary>
        /// Searches for a given propertyName on a JObject with maximum depth of 10 nested json objects.
        /// </summary>
        public static JToken FindToken(this JToken token, string propertyName)
        {
            var self = token;
            if (token.Type == JTokenType.Property)
            {
                self = token.Value<JProperty>().Value;
            }
            return FindPropertyRecursive(self, propertyName);
        }

        // Searches for a given propertyName on a JObject with maximum depth of 10 nested json objects.
        private static JToken FindPropertyRecursive(JToken token, string propertyName, int maxDepth = 10)
        {
            if (maxDepth == 0)
            {
                return null;
            }

            if (token.Type == JTokenType.Object)
            {
                foreach (JProperty prop in token.Children<JProperty>())
                {
                    if (prop.Name == propertyName)
                    {
                        return prop.Value;
                    }

                    JToken result = FindPropertyRecursive(prop.Value, propertyName, maxDepth - 1);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                foreach (JToken item in token.Children())
                {
                    JToken result = FindPropertyRecursive(item, propertyName, maxDepth - 1);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }
    }
}
