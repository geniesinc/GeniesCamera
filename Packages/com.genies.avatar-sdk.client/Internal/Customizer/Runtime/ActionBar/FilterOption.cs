using System;

namespace Genies.Customization.Framework.Actions
{
    [Serializable]
    public class FilterOption
    {
        public string displayName;
        public Action filterApplied;
    }
}