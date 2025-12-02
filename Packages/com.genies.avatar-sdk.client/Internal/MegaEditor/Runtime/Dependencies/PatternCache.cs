using System.Collections.Generic;
using Genies.Ugc;

namespace Genies.Looks.Customization.Utils.PatternCustomization
{
    public sealed class PatternCache
    {
        private readonly Dictionary<string, Pattern> _cache = new Dictionary<string, Pattern>();

        public void CachePattern(Pattern pattern)
        {
            string patternId = pattern?.TextureId;
            if (string.IsNullOrEmpty(patternId))
            {
                return;
            }

            if (!_cache.TryGetValue(patternId, out Pattern cachedPattern))
            {
                _cache[patternId] = cachedPattern = new Pattern();
            }

            pattern.DeepCopy(cachedPattern);
        }

        public bool TryGetCachedPattern(string patternId, out Pattern pattern)
        {
            if (patternId != null)
            {
                return _cache.TryGetValue(patternId, out pattern);
            }

            pattern = null;
            return false;
        }

        public void ClearCache()
        {
            _cache.Clear();
        }
    }
}
