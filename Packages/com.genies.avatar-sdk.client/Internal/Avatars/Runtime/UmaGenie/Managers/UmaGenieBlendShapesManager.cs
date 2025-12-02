using System;
using UMA;

namespace Genies.Avatars
{
    internal sealed class UmaGenieBlendShapesManager : IDisposable
    {
        // dependencies
        private readonly UmaGenie _genie;
        
        public UmaGenieBlendShapesManager(UmaGenie genie)
        {
            _genie = genie;
        }
        
        public void SetBlendShape(string name, float value)
        {
            if (name is null || (TryGetBlendShape(name, out BlendShapeData blendShape) && blendShape.value == value))
            {
                return;
            }

            _genie.Avatar.umaData.SetBlendShape(name, value, allowRebuild: false);
            bool isBaked = TryGetBlendShape(name, out blendShape) && blendShape.isBaked; // try to get the blend shape again as it could have been created in the previous line
            
            // only set the build dirty if the blend shape is baked
            if (isBaked)
            {
                _genie.SetUmaAvatarDirty();
            }
        }
        
        public void SetBlendShape(string name, float value, bool baked)
        {
            if (name is null || (TryGetBlendShape(name, out BlendShapeData blendShape) && blendShape.isBaked == baked && blendShape.value == value))
            {
                return;
            }

            bool wasBaked = blendShape?.isBaked ?? false;
            _genie.Avatar.umaData.SetBlendShapeData(name, baked, rebuild: false);
            _genie.Avatar.umaData.SetBlendShape(name, value);
            
            // only set the build dirty if the blend shape is baked or it was baked before
            if (baked || wasBaked)
            {
                _genie.SetUmaAvatarDirty();
            }
        }

        public float GetBlendShape(string name)
        {
            return TryGetBlendShape(name, out BlendShapeData blendShape) ? blendShape.value : 0.0f;
        }

        public bool RemoveBlendShape(string name)
        {
            if (!ContainsBlendShape(name))
            {
                return false;
            }

            _genie.Avatar.umaData.RemoveBlendShapeData(name, rebuild: false);
            _genie.SetUmaAvatarDirty();
            return true;
        }

        public bool IsBlendShapeBaked(string name)
        {
            return TryGetBlendShape(name, out BlendShapeData blendShape) && blendShape.isBaked;
        }

        public bool ContainsBlendShape(string name)
        {
            return TryGetBlendShape(name, out _);
        }

        public void Dispose()
        {
        }
        
        private bool TryGetBlendShape(string name, out BlendShapeData blendShape)
        {
            if (name is null)
            {
                blendShape = null;
                return false;
            }
            
            return _genie.Avatar.umaData.blendShapeSettings.blendShapes.TryGetValue(name, out blendShape);
        }
    }
}