using System;
using System.Collections.Generic;
using Genies.Services.Configs;

namespace Genies.FeatureFlags
{
    /// <summary>
    /// File that contains every key/value for the feature flags
    /// </summary>
    ///
    [Serializable]
    public class FeatureFlagsFileData
    {
        public Dictionary<BackendEnvironment, Dictionary<string, bool>> Data { get; private set; }

        public FeatureFlagsFileData()
        {
            Data = new Dictionary<BackendEnvironment, Dictionary<string, bool>>();
        }

        public void SetDataPerEnvironment(BackendEnvironment environmentId, Dictionary<string, bool> flags)
        {
            //validate if we need to update or add a new env
            if (Data.TryGetValue(environmentId, out _))
            {
                Data[environmentId] = flags;
            }
            else
            {
                Data.Add(environmentId, flags);
            }
        }
    }
}
