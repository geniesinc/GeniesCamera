using UnityEngine;

namespace Genies.UIFramework.Widgets
{
    public class PictureInPictureCameraProvider
    {
        public Camera Camera { get; }

        public PictureInPictureCameraProvider(Camera camera)
        {
            this.Camera = camera;
        }
    }
}
