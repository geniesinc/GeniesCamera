using System;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Utilities
{
    /// <summary>
    /// Components to configure the video render texture.
    /// </summary>
    [Serializable]
    public class VideoTextureRendererComponents
    {
        [SerializeField] private RawImage targetImage;
        [SerializeField] private RectTransform previewGroup;
        [SerializeField] private RectTransform videoGroup;
        [SerializeField] private int videoWidth;
        [SerializeField] private int videoHeight;

        public int VideoWidth => videoWidth == 0 ? Screen.width : videoWidth;
        public int VideoHeight => videoHeight == 0 ? Screen.height : videoHeight;
        public RawImage TargetImage => targetImage;
        public RectTransform PreviewGroup => previewGroup;
        public RectTransform VideoGroup => videoGroup;
    }
}
