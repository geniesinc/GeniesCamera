using System;
using Newtonsoft.Json;

namespace Genies.UGCW.Data.DecoratedSkin
{
    [Serializable]
    public class BaseSkinDefinition : IEquatable<BaseSkinDefinition>
    {
        [JsonProperty("MetallicSmoothness")] 
        public string MetallicSmoothness = string.Empty;
        [JsonProperty("Normal")] 
        public string Normal = string.Empty;
        [JsonProperty("Translucency")]
        public string Translucency = string.Empty;
        [JsonProperty("SkinColor")]
        public string SkinColor = string.Empty;

        public bool Equals(BaseSkinDefinition baseDefinition)
        {
            if (baseDefinition is null)
            {
                return false;
            }

            if (ReferenceEquals(this, baseDefinition))
            {
                return true;
            }

            if (GetType() != baseDefinition.GetType())
            {
                return false;
            }

            return (
                String.Equals(MetallicSmoothness, baseDefinition.MetallicSmoothness) &&
                String.Equals(Normal, baseDefinition.Normal) &&
                String.Equals(Translucency, baseDefinition.Translucency) &&
                String.Equals(SkinColor,baseDefinition.SkinColor)
            );
        }

        public override bool Equals(object obj) => this.Equals(obj as BaseSkinDefinition);
        
        public override int GetHashCode()
        {
            return HashCode.Combine(MetallicSmoothness, Normal, Translucency, SkinColor);
        }
    }
}