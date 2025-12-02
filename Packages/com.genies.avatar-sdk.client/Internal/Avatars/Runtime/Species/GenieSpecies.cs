using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Genies.Avatars
{
    /// <summary>
    /// Provides static information about all the Genie species.
    /// </summary>
    public static class GenieSpecies
    {
        public const string Unified = "unified";
        public const string UnifiedGAP = "unifiedGAP";
        public const string Dolls = "dolls";

        public static readonly IReadOnlyList<string> All =
            typeof(GenieSpecies)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(fieldInfo =>fieldInfo.IsLiteral && !fieldInfo.IsInitOnly)
                .Select(fieldInfo => fieldInfo.GetRawConstantValue() as string)
                .ToList()
                .AsReadOnly();
        
        public static OutfitSlotsData GetOutfitSlotsData(string species)
        {
            return species switch
            {
                Unified => UnifiedOutfitSlotsData.Instance,
                UnifiedGAP => UnifiedOutfitSlotsData.Instance,
                _ => null,
            };
        }
    }
}