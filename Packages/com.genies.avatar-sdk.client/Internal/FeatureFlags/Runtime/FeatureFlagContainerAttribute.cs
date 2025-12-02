using System;

namespace Genies.FeatureFlags
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class FeatureFlagsContainerAttribute : Attribute
    {
        public int Order { get; }

        public FeatureFlagsContainerAttribute(int order = -1)
        {
            Order = order;
        }
    }
}
