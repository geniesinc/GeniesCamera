using System;
using Newtonsoft.Json;

namespace Genies.UGCW.Data.DecoratedSkin
{
    [Serializable]
    public class MakeupDefinition : IEquatable<MakeupDefinition>
    {
        #region FRECKLES
        [JsonProperty("Freckles")] 
        public string Freckles = string.Empty;
        [JsonProperty("FrecklesColor")]
        public string FrecklesColor = string.Empty;
        [JsonProperty("FrecklesOpacity")] 
        public float FrecklesOpacity;
        #endregion
        
        #region BLUSH
        [JsonProperty("Blush")] 
        public string Blush = string.Empty;
        [JsonProperty("BlushColor1")]
        public string BlushColor1 = string.Empty;
        [JsonProperty("BlushColor2")]
        public string BlushColor2 = string.Empty;
        [JsonProperty("BlushColor3")]
        public string BlushColor3 = string.Empty;
        [JsonProperty("BlushOpacity")] 
        public float BlushOpacity;
        #endregion
        
        #region LIPSTICK
        [JsonProperty("Lipstick")] 
        public string Lipstick = string.Empty;
        [JsonProperty("LipstickColor1")]
        public string LipstickColor1 = string.Empty;
        [JsonProperty("LipstickColor2")]
        public string LipstickColor2 = string.Empty;
        [JsonProperty("LipstickColor3")]
        public string LipstickColor3 = string.Empty;
        #endregion
        
        #region EYESHADOW
        [JsonProperty("Eyeshadow")] 
        public string Eyeshadow = string.Empty;
        [JsonProperty("EyeshadowColor1")]
        public string EyeshadowColor1 = string.Empty;
        [JsonProperty("EyeshadowColor2")]
        public string EyeshadowColor2 = string.Empty;
        [JsonProperty("EyeshadowColor3")]
        public string EyeshadowColor3 = string.Empty;
        #endregion
        
        #region FACEGEMS
        [JsonProperty("FaceGems")] 
        public string FaceGems = string.Empty;
        [JsonProperty("FaceGemsColor1")]
        public string FaceGemsColor1 = string.Empty;
        [JsonProperty("FaceGemsColor2")]
        public string FaceGemsColor2 = string.Empty;
        [JsonProperty("FaceGemsColor3")]
        public string FaceGemsColor3 = string.Empty;
        [JsonProperty("FaceGemsOpacity")] 
        public float FaceGemsOpacity;
        #endregion
        
        #region STICKERS
        [JsonProperty("Stickers")] 
        public string Stickers = string.Empty;
        #endregion
       
        
        public bool Equals(MakeupDefinition makeupDefinition)
        {
            if (makeupDefinition is null)
            {
                return false;
            }

            if (ReferenceEquals(this, makeupDefinition))
            {
                return true;
            }

            if (GetType() != makeupDefinition.GetType())
            {
                return false;
            }

            return (
                String.Equals(Freckles, makeupDefinition.Freckles) &&
                String.Equals(FrecklesColor ,makeupDefinition.FrecklesColor) &&
                FrecklesOpacity == makeupDefinition.FrecklesOpacity  &&
                
                String.Equals(Blush, makeupDefinition.Blush) &&
                String.Equals(BlushColor1,makeupDefinition.BlushColor1) &&
                String.Equals(BlushColor2,makeupDefinition.BlushColor2) &&
                String.Equals(BlushColor3,makeupDefinition.BlushColor3) &&
                BlushOpacity == makeupDefinition.BlushOpacity &&
                
                String.Equals(Lipstick, makeupDefinition.Lipstick) &&
                String.Equals(LipstickColor1, makeupDefinition.LipstickColor1) &&
                String.Equals(LipstickColor2,makeupDefinition.LipstickColor2) &&
                String.Equals(LipstickColor3, makeupDefinition.LipstickColor3) &&
                
                String.Equals(Eyeshadow, makeupDefinition.Eyeshadow) &&
                String.Equals(EyeshadowColor1, makeupDefinition.EyeshadowColor1) &&
                String.Equals(EyeshadowColor2,makeupDefinition.EyeshadowColor2) &&
                String.Equals(EyeshadowColor3, makeupDefinition.EyeshadowColor3) &&
                
                String.Equals(FaceGems, makeupDefinition.FaceGems) &&
                String.Equals(FaceGemsColor1, makeupDefinition.FaceGemsColor1) &&
                String.Equals(FaceGemsColor2, makeupDefinition.FaceGemsColor2) &&
                String.Equals(FaceGemsColor3, makeupDefinition.FaceGemsColor3) &&
                FaceGemsOpacity ==  makeupDefinition.FaceGemsOpacity &&
                
                String.Equals(Stickers, makeupDefinition.Stickers)
            );
        }
        
        public void DeepCopy(MakeupDefinition destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.Eyeshadow = Eyeshadow;
            destination.EyeshadowColor1 = EyeshadowColor1;
            destination.EyeshadowColor2 = EyeshadowColor2;
            destination.EyeshadowColor3 = EyeshadowColor3;
            
            destination.Lipstick = Lipstick;
            destination.LipstickColor1 = LipstickColor1;
            destination.LipstickColor2 = LipstickColor2;
            destination.LipstickColor3 = LipstickColor3;
            
            destination.FaceGems = FaceGems;
            destination.FaceGemsColor1 = FaceGemsColor1;
            destination.FaceGemsColor2 = FaceGemsColor2;
            destination.FaceGemsColor3 = FaceGemsColor3;
            destination.FaceGemsOpacity = FaceGemsOpacity;
            
            destination.Blush = Blush;
            destination.BlushColor1 = BlushColor1;
            destination.BlushColor2 = BlushColor2;
            destination.BlushColor3 = BlushColor3;
            destination.BlushOpacity = BlushOpacity;
            
            destination.Freckles = Freckles;
            destination.FrecklesColor = FrecklesColor;
            destination.FrecklesOpacity = FrecklesOpacity;
            
            destination.Stickers = Stickers;
        }
        

        public override bool Equals(object obj) => this.Equals(obj as MakeupDefinition);
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Eyeshadow,  
                Lipstick,  
                FaceGems, FaceGemsOpacity, 
                Blush, BlushOpacity, 
                Freckles, 
                Stickers);
        }
    }
}