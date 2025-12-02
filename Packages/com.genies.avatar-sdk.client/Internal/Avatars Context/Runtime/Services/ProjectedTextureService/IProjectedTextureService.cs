using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.Ugc;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public interface IProjectedTextureService
    {
        /// <summary>
        /// Creates ProjectedTexture object and uploads its texture to s3
        /// </summary>
        /// <param name="newProjection">new projection texture</param>
        /// <returns>ProjectedTexture in case of success, null in case of failure</returns>
        UniTask<ProjectedTexture> CreateProjectedTextureAsync(Texture2D newProjection);

        /// <summary>
        /// Load the resources for a ProjectedTexture
        /// </summary>
        /// <param name="projection"></param>
        /// <returns>Texture2D or null</returns>
        UniTask<Ref<Texture2D>> LoadProjectedTextureAsync(ProjectedTexture projection);

        /// <summary>
        /// Delete the resources of a ProjectedTexture
        /// </summary>
        /// <param name="projection"></param>
        /// <returns>boolean success</returns>
        UniTask<bool> DeleteProjectedTextureAsync(ProjectedTexture projection);
    }
}
