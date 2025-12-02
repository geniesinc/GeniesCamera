using System;
using Cysharp.Threading.Tasks;
using Genies.S3Service;
using Genies.CrashReporting;
using Genies.Refs;
using Genies.Ugc;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public class ProjectedTextureRemoteLoaderService : IProjectedTextureService
    {
        private const string _projectedTextureKey = "projected-texture";

        // dependencies
        private readonly IGeniesS3Service _s3Service;
        private readonly ImageLoader _imageLoader;

        public ProjectedTextureRemoteLoaderService(IGeniesS3Service s3Service, ImageLoader imageLoader)
        {
            _s3Service = s3Service;
            _imageLoader = imageLoader;
        }

        public async UniTask<ProjectedTexture> CreateProjectedTextureAsync(Texture2D newProjection)
        {
            if (newProjection == null)
            {
                return null;
            }

            var projectionId = $"{_projectedTextureKey}-{DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH'-'mm'-'ss.fffffffK")}-{Guid.NewGuid().ToString()}";
            try
            {
                var bytes = newProjection.EncodeToPNG();
                var s3path = String.Format("ProjectedTextures/{0}.png", projectionId);
                var distributionUrl = await _s3Service.UploadObject(s3path, bytes);

                if (string.IsNullOrEmpty(distributionUrl))
                {
                    return null;
                }

                var projectedTexture = new ProjectedTexture();
                projectedTexture.ProjectionId = projectionId;
                projectedTexture.ProjectionRemoteUrl = distributionUrl;
                return projectedTexture;
            }
            catch (AggregateException ae)
            {
                CrashReporter.LogHandledException(ae);
                return default;
            }
        }

        public async UniTask<Ref<Texture2D>> LoadProjectedTextureAsync(ProjectedTexture projection)
        {
            if (projection == null || String.IsNullOrEmpty(projection.ProjectionRemoteUrl))
            {
                return default;
            }

            try
            {
                var result = await _s3Service.DownloadObject(projection.ProjectionRemoteUrl,
                                                             $"{projection.ProjectionId}.png");
                if (!result.wasDownloaded)
                {
                    return default;
                }

                string filePath = result.downloadedFilePath;
                Ref<Texture2D> texture = await _imageLoader.LoadImageAsTextureAsync(filePath);
                if (texture.IsAlive)
                {
                    texture.Item.name = projection.ProjectionId;
                }

                return texture;
            }
            catch (AggregateException ae)
            {
                CrashReporter.LogHandledException(ae);
                return default;
            }

        }

        public UniTask<bool> DeleteProjectedTextureAsync(ProjectedTexture projection)
        {
            try
            {
                _s3Service.DeleteObject(projection.ProjectionId);

                return UniTask.FromResult(true);
            }
            catch (AggregateException ae)
            {
                CrashReporter.LogHandledException(ae);
            }

            return UniTask.FromResult(false);
        }
    }
}
