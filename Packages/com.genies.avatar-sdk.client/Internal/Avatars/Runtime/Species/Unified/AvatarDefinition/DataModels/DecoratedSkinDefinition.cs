using System;
using System.Linq;
using Genies.Avatars;
using Genies.Avatars.Definition.DataModels;
using Genies.UGCW.Data.DecoratedSkin;
using Newtonsoft.Json;

namespace Genies.UGCW.Data
{
    [Serializable]
    public class DecoratedSkinDefinition : AvatarFeatureDefinition, IEquatable<DecoratedSkinDefinition>
    {
        [JsonProperty("BaseSkin")]
        public BaseSkinDefinition BaseSkin = new BaseSkinDefinition();

        [JsonProperty("Makeup")] 
        public MakeupDefinition Makeup = new MakeupDefinition();
        
        [JsonProperty("Tattoos")]
        public TattooDefinition[] Tattoos = new TattooDefinition[8];    
        
        public bool Equals(DecoratedSkinDefinition skinDefinition)
        {
            if (skinDefinition is null)
            {
                return false;
            }

            if (ReferenceEquals(this, skinDefinition))
            {
                return true;
            }

            if (GetType() != skinDefinition.GetType())
            {
                return false;
            }

            if (Tattoos != null && Tattoos.Length > 0)
            {
                if (skinDefinition.Tattoos == null)
                {
                    return false;
                }

                if (Tattoos.Length != skinDefinition.Tattoos.Length)
                {
                    return false;
                }

                if (!Tattoos.SequenceEqual(skinDefinition.Tattoos))
                {
                    return false;
                }
            }
            
            return (
                JsonVersion == skinDefinition.JsonVersion &&
                BaseSkin.Equals(skinDefinition.BaseSkin) &&
                Makeup.Equals(skinDefinition.Makeup)
            );
        }
        
        public void DeepCopy(DecoratedSkinDefinition destination)
        {
            if (destination is null)
            {
                return;
            }

            destination.BaseSkin = BaseSkin;
            Makeup.DeepCopy(destination.Makeup);
            
            // TODO: create extension method
           for (int i = 0; i < Tattoos.Length; i++)
           {
               if (Tattoos[i] == null)
               {
                   destination.Tattoos[i] = null;
               }
               else
               {
                   destination.Tattoos[i] ??= new TattooDefinition();
                   Tattoos[i].DeepCopy(destination.Tattoos[i]);
               }
               
           }
        }
        
        public override bool Equals(object obj) => this.Equals(obj as DecoratedSkinDefinition);
        protected override string GetFeatureKey()
        {
            return AvatarFeatureType.DecoratedSkin;
        }

        protected override string GetDefinitionVersion()
        {
            return "1-0-0";
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(BaseSkin, Makeup, Tattoos);
        }
    }
}