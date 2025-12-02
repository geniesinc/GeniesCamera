using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.FeatureFlags
{

    public interface IFeatureFlagsManager
    {
        UniTask<Dictionary<string,bool>> GetAllFeatureFlagsStatus();

        void SetFeatureFlagOverride(string featureFlag, Func<bool> isEnabledGetter);
        void RemoveFeatureFlagOverride(string featureFlag);
        bool IsFeatureEnabled(string featureFlag);
    }
}
