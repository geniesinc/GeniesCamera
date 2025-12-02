using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    // 
    /// <summary>
    /// Material property IDs for an specific makeup slot
    /// </summary>
    public struct MakeupSlotPropertyIds
    {
        public int TextureId;
        public int[] ColorIds;
        
        public static bool TryGetPropertyIds(string slotId, out MakeupSlotPropertyIds propertyIds)
        {
            return PropertyIdsBySlotId.TryGetValue(slotId, out propertyIds);
        }
        
        private static readonly Dictionary<string, MakeupSlotPropertyIds> PropertyIdsBySlotId = new Dictionary<string, MakeupSlotPropertyIds>
        {
            {
                MakeupSlot.Stickers,
                new MakeupSlotPropertyIds
                {
                    TextureId = Shader.PropertyToID("_Stickers"),
                    ColorIds = null
                }
            },
            {
                MakeupSlot.Lipstick,
                new MakeupSlotPropertyIds
                {
                    TextureId = Shader.PropertyToID("_Lipstick"),
                    ColorIds = new []
                    {
                        Shader.PropertyToID("_LipstickColor1"),
                        Shader.PropertyToID("_LipstickColor2"),
                        Shader.PropertyToID("_LipstickColor3")
                    }
                }
            },
            {
                MakeupSlot.Freckles,
                new MakeupSlotPropertyIds
                {
                    TextureId = Shader.PropertyToID("_Freckles"),
                    ColorIds = new []
                    {
                        Shader.PropertyToID("_FrecklesColor"),
                    }
                }
            },
            {
                MakeupSlot.FaceGems,
                new MakeupSlotPropertyIds
                {
                    TextureId = Shader.PropertyToID("_FaceGems"),
                    ColorIds = new []
                    {
                        Shader.PropertyToID("_FaceGemsColor1"),
                        Shader.PropertyToID("_FaceGemsColor2"),
                        Shader.PropertyToID("_FaceGemsColor3")
                    }
                }
            },
            {
                MakeupSlot.Eyeshadow,
                new MakeupSlotPropertyIds
                {
                    TextureId = Shader.PropertyToID("_Eyeshadow"),
                    ColorIds = new []
                    {
                        Shader.PropertyToID("_EyeshadowColor1"),
                        Shader.PropertyToID("_EyeshadowColor2"),
                        Shader.PropertyToID("_EyeshadowColor3")
                    }
                }
            },
            {
                MakeupSlot.Blush,
                new MakeupSlotPropertyIds
                {
                    TextureId = Shader.PropertyToID("_Blush"),
                    ColorIds = new []
                    {
                        Shader.PropertyToID("_BlushColor1"),
                        Shader.PropertyToID("_BlushColor2"),
                        Shader.PropertyToID("_BlushColor3")
                    }
                }
            }
        };
    }
}