using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities
{
    public delegate Sprite SpriteCreator(Texture2D texture);

    /// <summary>
    /// Utility class for creating and caching Sprites created from textures at runtime.
    /// </summary>
    public class SpriteCache
    {
        public const int kDefaultMaxSize = 50;

        public int CachedSprites => _cache.Count;

        public SpriteCreator SpriteCreator;
        public uint MaxSize = 50;

        protected readonly Dictionary<Texture2D, Sprite> _cache = new Dictionary<Texture2D, Sprite>();
        protected readonly Queue<Texture2D> _textures = new Queue<Texture2D>();

        public SpriteCache()
            : this(null, kDefaultMaxSize) { }

        public SpriteCache(SpriteCreator spriteCreator)
            : this(spriteCreator, kDefaultMaxSize) { }

        public SpriteCache(uint maxSize)
            : this(null, maxSize) { }

        public SpriteCache(SpriteCreator spriteCreator, uint maxSize)
        {
            MaxSize = maxSize;
            SpriteCreator = spriteCreator;
        }

        public Sprite GetSprite(Texture2D texture, SpriteCreator spriteCreator = null)
        {
            if (!texture)
            {
                return null;
            }

            // also check that the sprite was not destroyed by external actors, in that case we need to create it again
            if (_cache.TryGetValue(texture, out Sprite sprite) && sprite)
            {
                return sprite;
            }

            if (spriteCreator != null)
            {
                sprite = spriteCreator(texture);
            }
            else if (SpriteCreator != null)
            {
                sprite = SpriteCreator(texture);
            }
            else
            {
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }

            // if the sprite was cached but destroyed, we don't need to add the texture to the queue again
            if (!_cache.ContainsKey(texture))
            {
                _textures.Enqueue(texture);
            }

            _cache[texture] = sprite;

            if (MaxSize > 0 && _cache.Count > MaxSize)
            {
                ClearOldestSprites(1);
            }

            return sprite;
        }

        public void ClearOldestSprites(int count)
        {
            for (int i = 0; i < count && _cache.Count > 0; ++i)
            {
                Texture2D oldestTexture = _textures.Dequeue();

                Sprite oldestSprite = _cache[oldestTexture];
                _cache.Remove(oldestTexture);

                if (oldestSprite)
                {
                    Object.Destroy(oldestSprite);
                }
            }
        }

        public void ClearCache()
        {
            foreach (Sprite sprite in _cache.Values)
            {
                if (sprite)
                {
                    Object.Destroy(sprite);
                }
            }

            _cache.Clear();
            _textures.Clear();
        }

        ~SpriteCache()
        {
            ClearCache();
        }
    }
}
