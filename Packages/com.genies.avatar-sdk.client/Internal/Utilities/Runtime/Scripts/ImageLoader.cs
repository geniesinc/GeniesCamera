using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Refs;
using Genies.ServiceManagement;
using UnityEngine;
using UnityEngine.Networking;
using VContainer;

namespace Genies.Utilities
{
    [AutoResolve]
    public class ImageLoaderInstaller : IGeniesInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ImageLoader>(Lifetime.Singleton);
        }
    }
    
    /// <summary>
    /// Capable of loading images from a given URL (can point to the device's file system), either as Sprite or Texture2D assets.
    /// It caches already loaded URLs so the same image is not loaded twice.
    /// </summary>
    public sealed class ImageLoader : IDisposable
    {
        private delegate UniTask<T> LoadOperation<T>(string url, CancellationToken cancellationToken = default);

        private readonly HandleCache<string, Texture2D> _textureCache = new();
        private readonly HandleCache<string, Sprite> _spriteCache = new();
        private readonly Dictionary<string, UniTaskCompletionSource> _loadingTasks = new();

        private CancellationTokenSource CtsInternal { get; } = new();
        
        private bool IsDisposed { get; set; }

        ~ImageLoader()
        {
            Dispose(false);
        }
        
        public void Dispose()
        {
            if (IsDisposed) { return; }
            IsDisposed = true;
            
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                CtsInternal.Cancel();
                CtsInternal.Dispose();
                
                foreach (var task in _loadingTasks.Values)
                {
                    task?.TrySetCanceled();
                }
                _loadingTasks.Clear();
                
                _textureCache.Clear();
                _spriteCache.Clear();
            }
        }

        public UniTask<Ref<Sprite>> LoadImageAsSpriteAsync(string url, CancellationToken cancellationToken = default)
            => WaitLoadOperationAndInvokeAsync(LoadImageAsSpriteOperation, url, cancellationToken);

        /// <summary>
        /// Load a sprite and don't use the local cache
        /// </summary>
        public UniTask<Ref<Sprite>> LoadImageAsSpriteNoCacheAsync(string url, CancellationToken cancellationToken = default)
            => WaitLoadOperationAndInvokeAsync(LoadImageAsSpriteNoCacheOperation, url, cancellationToken); 

        public UniTask<Ref<Texture2D>> LoadImageAsTextureAsync(string url, CancellationToken cancellationToken = default)
            => WaitLoadOperationAndInvokeAsync(LoadImageAsTextureOperation, url, cancellationToken);

        /// <summary>
        /// Load a texture and don't use the local cache
        /// </summary>
        public UniTask<Ref<Texture2D>> LoadImageAsTextureNoCacheAsync(string url, CancellationToken cancellationToken = default)
            => WaitLoadOperationAndInvokeAsync(LoadImageAsTextureNoCacheOperation, url, cancellationToken);

        // wraps the call to LoadOperation methods so we avoid parallel loads for the same URL
        private async UniTask<T> WaitLoadOperationAndInvokeAsync<T>(LoadOperation<T> load, string url, CancellationToken cancellationToken = default)
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException("Instance has been disposed and can no longer be used.");
            }
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CtsInternal.Token);
            try
            {
                cts.Token.ThrowIfCancellationRequested();

                // if the url is a local file path then make sure that it has the file:// protocol prefix so the UnityWebRequest takes care of loading it
                if (File.Exists(url) && !url.StartsWith("file://"))
                {
                    url = $"file://{url}";
                }

                // if there is already a load operation to the given url, then wait before invoking the given load operation
                UniTaskCompletionSource completionSource;

                if (_loadingTasks.TryGetValue(url, out completionSource))
                {
                    await completionSource.Task;
                }

                cts.Token.ThrowIfCancellationRequested();

                completionSource = new UniTaskCompletionSource();
                cts.Token.Register(() =>
                {
                    completionSource.TrySetCanceled(cts.Token);
                });
                _loadingTasks[url] = completionSource;

                // invoke the load operation
                T result = await load(url, cancellationToken);

                // notify to other awaiters that the load operation finished
                completionSource.TrySetResult();

                return result;
            }
            catch (OperationCanceledException oce)
            {
                Debug.LogException(oce);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
            finally
            {
                _loadingTasks.Remove(url);
            }
        }

        private async UniTask<Ref<Sprite>> LoadImageAsSpriteOperation(string url, CancellationToken cancellationToken = default)
        {
            // check if the texture was loaded before and has not been disposed yet
            if (_spriteCache.TryGetNewReference(url, out Ref<Sprite> spriteRef))
            {
                return spriteRef;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                Ref<Texture2D> textureRef = await LoadImageAsTextureOperation(url, cancellationToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                
                if (!textureRef.IsAlive)
                {
                    return default;
                }

                // create the sprite from the texture
                Texture2D texture = textureRef.Item;
                var rect = new Rect(0, 0, texture.width, texture.height);
                var sprite = Sprite.Create(texture, rect, new Vector2(0.5F, 0.5F), Mathf.Max(texture.width, texture.height), 0, SpriteMeshType.FullRect, Vector4.zero, false);

                // create the ref and cache its handle
                spriteRef = CreateRef.FromUnityObject(sprite);
                spriteRef = CreateRef.FromDependentResource(spriteRef, textureRef);
                _spriteCache.CacheHandle(url, spriteRef);

                return spriteRef;
            }
            catch (OperationCanceledException oce)
            {
                Debug.LogException(oce);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }
        
        private async UniTask<Ref<Sprite>> LoadImageAsSpriteNoCacheOperation(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                Ref<Texture2D> textureRef = await LoadImageAsTextureNoCacheOperation(url, cancellationToken);
                
                cancellationToken.ThrowIfCancellationRequested();
                
                if (!textureRef.IsAlive)
                {
                    return default;
                }

                // create the sprite from the texture
                Texture2D texture = textureRef.Item;
                var rect = new Rect(0, 0, texture.width, texture.height);
                var sprite = Sprite.Create(texture, rect, new Vector2(0.5F, 0.5F), Mathf.Max(texture.width, texture.height), 0, SpriteMeshType.FullRect, Vector4.zero, false);

                // create the ref and cache its handle
                var spriteRef = CreateRef.FromUnityObject(sprite);
                spriteRef = CreateRef.FromDependentResource(spriteRef, textureRef);

                return spriteRef;
            }
            catch (OperationCanceledException oce)
            {
                Debug.LogException(oce);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        private async UniTask<Ref<Texture2D>> LoadImageAsTextureOperation(string url, CancellationToken cancellationToken = default)
        {
            // check if the texture was loaded before and has not been disposed yet
            if (_textureCache.TryGetNewReference(url, out Ref<Texture2D> textureRef))
            {
                return textureRef;
            }

            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await request.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (UnityWebRequestException)
            {
                if (request.error.Contains("Forbidden"))
                {
                    // Known handled issue: Error: "HTTP/1.1 403 Forbidden". It happens whenever a profilePic is not yet uploaded or backend-processed.
                    // Temporarily lowered to console LogWarning as, after testing, it may otherwise unnecessarily consume a lot of our Sentry error quota.
                    Debug.LogWarning($"[BackendAssetLoader] Access forbidden when loading an image from: \"{url}\". Error: {request.error}");
                }
                else
                {
                    CrashReporter.Log($"[BackendAssetLoader] WebRequestException. Could not load the image file from \"{url}\". Error: {request.error}", LogSeverity.Error);
                }
                return default;
            }
            catch (OperationCanceledException oce)
            {
                Debug.LogException(oce);
                return default;
            }
            catch (Exception exception)
            {
                CrashReporter.LogHandledException(exception);
                return default;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                CrashReporter.Log($"[BackendAssetLoader] NonSuccessfulResult. Could not load the image file from \"{url}\". Error: {request.error}", LogSeverity.Error);
                return default;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            texture.filterMode = FilterMode.Bilinear;

            // create the ref and cache its handle
            textureRef = CreateRef.FromUnityObject(texture);
            _textureCache.CacheHandle(url, textureRef);

            return textureRef;
        }
        
        //load the texture, don't use the cache
        private async UniTask<Ref<Texture2D>> LoadImageAsTextureNoCacheOperation(string url, CancellationToken cancellationToken = default)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await request.SendWebRequest().WithCancellation(cancellationToken);
            }
            catch (UnityWebRequestException)
            {
                if (request.error.Contains("Forbidden"))
                {
                    // Known handled issue: Error: "HTTP/1.1 403 Forbidden". It happens whenever a profilePic is not yet uploaded or backend-processed.
                    // Temporarily lowered to console LogWarning as, after testing, it may otherwise unnecessarily consume a lot of our Sentry error quota.
                    Debug.LogWarning($"[BackendAssetLoader] Access forbidden when loading an image from: \"{url}\". Error: {request.error}");
                }
                else
                {
                    CrashReporter.Log($"[BackendAssetLoader] WebRequestException. Could not load the image file from \"{url}\". Error: {request.error}", LogSeverity.Error);
                }
                return default;
            }
            catch (OperationCanceledException oce)
            {
                Debug.LogException(oce);
                return default;
            }
            catch (Exception exception)
            {
                CrashReporter.LogHandledException(exception);
                return default;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                CrashReporter.Log($"[BackendAssetLoader] NonSuccessfulResult. Could not load the image file from \"{url}\". Error: {request.error}", LogSeverity.Error);
                return default;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            texture.filterMode = FilterMode.Bilinear;

            // create the ref and cache its handle
            var textureRef = CreateRef.FromUnityObject(texture);

            return textureRef;
        }
    }
}
