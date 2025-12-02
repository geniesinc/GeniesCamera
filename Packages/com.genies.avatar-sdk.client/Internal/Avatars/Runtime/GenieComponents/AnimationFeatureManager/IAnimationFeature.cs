using Genies.Utilities;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars
{
    public interface IAnimationFeature
    {
        bool SupportsParameters(AnimatorParameters parameters);
        GenieComponent CreateFeatureComponent(AnimatorParameters parameters);

        bool TrySerialize(out JToken token)
            => SerializerAs<IAnimationFeature>.TrySerialize(this, out token);
    }
}