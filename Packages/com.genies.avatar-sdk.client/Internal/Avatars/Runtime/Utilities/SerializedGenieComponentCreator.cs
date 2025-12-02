using Genies.Utilities;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IGenieComponentCreator"/> implementation that creates the component instances from a serialized
    /// component token.
    /// </summary>
    public sealed class SerializedGenieComponentCreator : IGenieComponentCreator
    {
        private readonly JToken _token;

        public SerializedGenieComponentCreator(JToken token)
        {
            _token = token;
        }

        public GenieComponent CreateComponent()
        {
            return SerializerAs<GenieComponent>.TryDeserialize(_token, out GenieComponent component) ? component : null;
        }
    }
}