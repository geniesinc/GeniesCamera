using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
    public class BlendshapeDataFacePresetContainer : OrderedScriptableObject
    {
        public Texture2D maleIcon;
        public Texture2D femaleIcon;
        public Texture2D unifiedIcon;
        public string facePresetIdentifier;
        public List<string> blendShapeIds = new();
    }
}
