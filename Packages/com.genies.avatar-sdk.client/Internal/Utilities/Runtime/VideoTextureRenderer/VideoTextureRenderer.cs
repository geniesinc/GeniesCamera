using UnityEngine;
using UnityEngine.UI;

namespace Genies.Utilities
{
    /// <summary>
    /// Renders the camera view as video on a render texture.
    /// </summary>
    /// <remarks>
    /// Used for the video preview on the exporting page.
    /// </remarks>
    public class VideoTextureRenderer
    {
        private readonly int _videoWidth;
        private readonly int _videoHeight;
        private readonly RawImage _targetImage;
        private readonly RectTransform _previewGroup;
        private readonly RectTransform _videoGroup;

        private Camera _camera;

        private RenderTexture _viewTexture;

        /// <summary>
        /// Renders the MainCamera's view onto a render texture.
        /// </summary>
        /// <param name="targetCamera">The target camera to use</param>
        /// <param name="components">the configuration of the render texture</param>
        public VideoTextureRenderer(Camera targetCamera, VideoTextureRendererComponents components)
        {
            _camera = targetCamera;
            _videoWidth = components.VideoWidth;
            _videoHeight = components.VideoHeight;
            _targetImage = components.TargetImage;
            _previewGroup = components.PreviewGroup;
            _videoGroup = components.VideoGroup;

            ConfigTargetImage();
        }

        /// <summary>
        /// Resets the camera target texture.
        /// </summary>
        public void CleanCamera()
        {
            _camera.targetTexture = null;
        }

        private void ConfigTargetImage()
        {
            //Rescale video preview dimensions to match preview group max possible dimensions
            var targetVideoPreviewWidth = _videoWidth * _previewGroup.sizeDelta.y / _videoHeight;

            //Apply new dimensions to size delta
            var sizeDelta = _videoGroup.sizeDelta;
            sizeDelta = new Vector2(targetVideoPreviewWidth, sizeDelta.y);
            _videoGroup.sizeDelta = sizeDelta;

            //Render texture from camera
            _viewTexture = new RenderTexture(_videoWidth, _videoHeight, 0);
            _targetImage.texture = _viewTexture;
            _camera.targetTexture = _viewTexture;
            _camera.Render();
        }
    }
}
