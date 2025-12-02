using System;

namespace Genies.Customization.Framework
{
    [Flags]
    public enum CustomizerViewFlags
    {
        None = 0,
        NavBar = 1 << 0,
        ActionBar = 1 << 1,
        Breadcrumbs = 1 << 2,
        CustomizationEditor = 1 << 3
    }
    
    public static class CustomizerViewFlagsExtensions
    {
        public static bool HasFlagFast(this CustomizerViewFlags value, CustomizerViewFlags flag)
        {
            return (value & flag) != 0;
        }
    }

}