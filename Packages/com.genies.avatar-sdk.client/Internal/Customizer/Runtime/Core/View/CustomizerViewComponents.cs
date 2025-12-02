using System;
using Genies.UI.Animations;
using UnityEngine;

namespace Genies.Customization.Framework
{
    [Serializable]
    public class CustomizerViewComponents
    {
        public RectTransform rectTransform;
        public CanvasGroup canvasGroup;
        public float Height => rectTransform.sizeDelta.y;

        public void TerminateAnimations()
        {
            canvasGroup.Terminate();
            rectTransform.Terminate();
        }
    }
}
