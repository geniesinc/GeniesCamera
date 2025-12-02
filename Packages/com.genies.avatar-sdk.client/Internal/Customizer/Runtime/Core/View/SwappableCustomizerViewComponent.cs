using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Customization.Framework
{
    [Serializable]
    public class SwappableCustomizerViewComponent : CustomizerViewComponents
    {
        public RectTransform backSwapLayer;
        public RectTransform frontSwapLayer;
        public float inYPivot;
        [FormerlySerializedAs("outYPivot")]
        public float outTopYPivot;
        public float outBottomYPivot;
    }
}
