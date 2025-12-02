using System;
using Genies.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public abstract class RepeatableParamBaseFeatureAsset : AnimationFeatureAsset
    {
        public virtual List<string> SupportSuffixes { get; protected set; }

        public override bool SupportsParameters(AnimatorParameters parameters)
        {
            // Input parameters must contain at least one instance of each suffix declared on this asset

            foreach (var suffix in SupportSuffixes)
            {
                if (!ParamsContainSuffix(suffix, parameters))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ParamsContainSuffix(string suffix, AnimatorParameters parameters)
        {
            foreach (var inParam in parameters)
            {
                if (inParam.name.EndsWith($"_{suffix}", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
