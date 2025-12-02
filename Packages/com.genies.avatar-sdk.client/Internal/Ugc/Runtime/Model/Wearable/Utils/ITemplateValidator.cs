
namespace Genies.Ugc
{
    public interface ITemplateValidator
    {
        void ValidateWearable(Wearable wearable, WearableTemplate template, bool validateSubModels = true);
        void ValidateSplit(Split split, SplitTemplate template, bool validateSubModels = true);
        void ValidateRegion(Region region, RegionTemplate template, bool validateSubModels = true);
        void ValidateStyle(Style style, StyleTemplate template, bool validateSubModels = true);
        void ValidatePattern(Pattern pattern, PatternTemplate template);
        Style CreateDefaultStyle();
        Pattern CreateDefaultPattern();
    }
}
