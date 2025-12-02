using System.Collections.Generic;
using Genies.Refs;
using GnWrappers;

using Texture = UnityEngine.Texture;

namespace Genies.Naf
{
    public static class MaterialTexturePropertiesExtensions
    {
        public delegate void PropertyProcessor(string name, GnWrappers.Texture texture);
        public delegate void TexturePropertyProcessor(string name, Texture texture);
        public delegate void TextureRefPropertyProcessor(string name, Ref<Texture> textureRef);
        
        public static void Process(this MaterialTextureProperties properties, PropertyProcessor processor)
        {
            uint size = properties.Size();
            for (uint i = 0; i < size; ++i)
            {
                using GnWrappers.Texture texture = properties.Value(i);
                processor(properties.Key(i), texture);
            }
        }
        
        /**
         * Process all the properties as Unity textures and returns a reference to all of them. You must keep the
         * reference and dispose it to release the textures.
         */
        public static Ref ProcessAsUnityTextures(this MaterialTextureProperties properties, TexturePropertyProcessor processor)
        {
            uint size = properties.Size();
            var textureRefs = new List<Ref<Texture>>((int)size);
            for (uint i = 0; i < size; ++i)
            {
                using GnWrappers.Texture texture = properties.Value(i);
                Ref<Texture> textureRef = texture.AsUnityTexture();
                
                processor(properties.Key(i), textureRef.Item);
                
                if (textureRef.IsAlive)
                {
                    textureRefs.Add(textureRef);
                }
            }
            
            // return a reference that encapsulates all texture references
            return CreateRef.FromDependentResource((byte)0, textureRefs);
        }
        
        /**
         * Process all the properties as Unity texture references.
         */
        public static void ProcessAsUnityTextures(this MaterialTextureProperties properties, TextureRefPropertyProcessor processor)
        {
            uint size = properties.Size();
            for (uint i = 0; i < size; ++i)
            {
                using GnWrappers.Texture texture = properties.Value(i);
                Ref<Texture> textureRef = texture.AsUnityTexture();
                processor(properties.Key(i), textureRef);
            }
        }
    }
}