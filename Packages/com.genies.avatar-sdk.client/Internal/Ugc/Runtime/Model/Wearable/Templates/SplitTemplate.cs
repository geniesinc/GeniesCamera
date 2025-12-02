using System.Collections.Generic;

namespace Genies.Ugc
{
    public class SplitTemplate
    {
        public string MaterialVersion;
        public readonly List<string> ElementIds;
        public Dictionary<string, List<RegionTemplate>> ElementRegionTemplates;
        
        private readonly HashSet<string> _elementIds;

        public SplitTemplate(List<string> elementIds)
        {
            ElementIds = elementIds;
            _elementIds = new HashSet<string>(elementIds);
        }

        public bool IsElementIdAvailable(string elementId)
            => _elementIds.Contains(elementId);
    }
}
