using Newtonsoft.Json;

namespace Genies.UGCW.Data.DecoratedSkin
{
    public static class DecoratedSkinExtensions 
    {
        public static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };

        public static string SerializeDefinition(this DecoratedSkinDefinition def)
        {
            return JsonConvert.SerializeObject(def, SerializerSettings);
        }
        
        public static DecoratedSkinDefinition DefaultDefinition()
        {
            var def = new DecoratedSkinDefinition
            {
                BaseSkin = new BaseSkinDefinition(),
                Makeup =  new MakeupDefinition(),
                // 8 = number of places that the user can put tattoos in the avatar bodypart
                Tattoos = new TattooDefinition[8]
            };
            return def;
        }
    }
}