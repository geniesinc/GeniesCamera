using System;
using Genies.Refs;
using UnityEngine;

using Object = UnityEngine.Object;
using Texture = UnityEngine.Texture;

namespace Genies.Naf
{
    public static class TextureExtensions
    {
        public static bool DebugLogCreatedTextures = false;
        
        private static readonly HandleCache<IntPtr, Texture> Cache = new();
        
        /**
         * Whether the texture is a native texture or not. A native texture is any Texture created from a Texture wrapper.
         */
        public static bool IsNative(this Texture texture)
        {
            return Cache.IsHandleCached(texture.GetNativeTexturePtr());
        }

        /**
         * If the texture is native, this will output a new texture reference to it.
         */
        public static bool TryGetNativeRef(this Texture texture, out Ref<Texture> textureRef)
        {
            return Cache.TryGetNewReference(texture.GetNativeTexturePtr(), out textureRef);
        }
        
        public static Ref<Texture> AsUnityTexture(this GnWrappers.Texture nativeTexture)
        {
            if (nativeTexture.IsNull())
            {
                return default;
            }

            IntPtr pointer = nativeTexture.Pointer();
            if (pointer == IntPtr.Zero)
            {
                return default;
            }

            if (Cache.TryGetNewReference(pointer, out Ref<Texture> textureRef))
            {
                return textureRef;
            }

            if (!VulkanFormat.TryGetTextureFormat(nativeTexture.Format(), out TextureFormat textureFormat, out bool linear))
            {
                Debug.LogError($"Couldn't identify Vulkan format {nativeTexture.Format()} for texture: {nativeTexture.Name()}");
                return default;
            }
            
            if (DebugLogCreatedTextures)
            {
                Debug.Log($"Creating new external Texture \"{nativeTexture.Name()}\":\n{GetInfo(nativeTexture, textureFormat, linear)}");
            }

            try
            {
                var texture = Texture2D.CreateExternalTexture(
                    (int)nativeTexture.Width(), (int)nativeTexture.Height(),
                    textureFormat,
                    false, // we have not implemented mipmaps yet on NAF
                    linear,
                    pointer
                );
                
                texture.name = nativeTexture.Name();
                
                /**
                 * Use our Refs package to create a texture reference that encapsulates the texture wrapper with our
                 * custom IResource implementation.
                 */
                nativeTexture = new GnWrappers.Texture(nativeTexture); // ensures we get our own native shared pointer to the texture (the caller must still own the original one)
                var resource = new NativeTextureResource(texture, nativeTexture);
                textureRef = CreateRef.From(resource);
                
                // cache the texture handle and return
                Cache.CacheHandle(pointer, textureRef);
                
                return textureRef;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception while creating external Texture \"{nativeTexture.Name()}\":\n{GetInfo(nativeTexture, textureFormat, linear)}\nException: {exception}");
                return default;
            }
        }

        private static string GetInfo(GnWrappers.Texture texture, TextureFormat format, bool linear)
        {
            return $"VkFormat: {VulkanFormat.GetName(texture.Format())}" +
                   $"\nTextureFormat: {format.ToString()}" +
                   $"\nsRGB: {!linear}" + 
                   $"\nPointer: {texture.Pointer()}";
        }
        
        private sealed class NativeTextureResource : IResource<Texture>
        {
            public Texture Resource { get; }
            
            private readonly GnWrappers.Texture _nativeTexture;

            public NativeTextureResource(Texture texture, GnWrappers.Texture nativeTexture)
            {
                Resource = texture;
                _nativeTexture = nativeTexture;
            }
            
            public void Dispose()
            {
                /**
                 * IMPORTANT: if you destroy the Unity texture after disposing the native texture, or change to Destroy
                 * instead of DestroyImmediate, the app can crash silently due to the shaders trying to access a texture
                 * that was released from GPU.
                 */
                Object.DestroyImmediate(Resource);
                _nativeTexture.Dispose();
            }
        }
    }
}