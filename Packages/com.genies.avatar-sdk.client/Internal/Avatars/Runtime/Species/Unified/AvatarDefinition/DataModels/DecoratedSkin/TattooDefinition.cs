using System;
using Newtonsoft.Json;

namespace Genies.UGCW.Data.DecoratedSkin
{
    [Serializable]
    public class TattooDefinition : IEquatable<TattooDefinition>
    {
        #region TATTOO

        [JsonProperty("ShaderVersion", Required = Required.Always)]
        public readonly string ShaderVersion = "1-0-0"; // TODO: check shader version for tattoos

        [JsonProperty("BodyPartId")] public string BodyPartId;
        [JsonProperty("Tattoo")] public string Tattoo = string.Empty;
        [JsonProperty("Name")] public string Name = string.Empty;
        [JsonProperty("PositionX")] public float PositionX;
        [JsonProperty("PositionY")] public float PositionY;
        [JsonProperty("Rotation")] public float Rotation;
        [JsonProperty("Scale")] public float Scale = 1f;

        #endregion

        public bool Equals(TattooDefinition tattooDefinition)
        {
            if (tattooDefinition is null)
            {
                return false;
            }

            if (ReferenceEquals(this, tattooDefinition))
            {
                return true;
            }

            if (GetType() != tattooDefinition.GetType())
            {
                return false;
            }

            return (
                  String.Equals(BodyPartId,tattooDefinition.BodyPartId) &&
                  String.Equals(ShaderVersion, tattooDefinition.ShaderVersion) &&
                  String.Equals(Tattoo, tattooDefinition.Tattoo) &&
                  String.Equals(Name, tattooDefinition.Name) &&
                  PositionX == tattooDefinition.PositionX &&
                  PositionY == tattooDefinition.PositionY &&
                  Rotation == tattooDefinition.Rotation &&
                  Scale == tattooDefinition.Scale
            );
        }

        public void DeepCopy(TattooDefinition destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.Tattoo = Tattoo;
            destination.Name = Name;
            destination.BodyPartId = BodyPartId;
            destination.Rotation = Rotation;
            destination.Scale = Scale;
            destination.PositionX = PositionX;
            destination.PositionY = PositionY;
        }
        
        public override bool Equals(object obj) => this.Equals(obj as TattooDefinition);
        
        public override int GetHashCode()
        {
            return HashCode.Combine(Tattoo, Name, BodyPartId, Rotation, Scale, PositionX, PositionY);;
        }
    }
}