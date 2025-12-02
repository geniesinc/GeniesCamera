using System;
using System.Collections.Generic;

namespace Genies.Utilities
{
    public readonly struct DefinitionConversionTarget
    {
        public readonly List<string> SourceVersions;
        public readonly string TargetVersion;

        /// <summary>
        ///
        /// </summary>
        /// <param name="sourceVersions">NOTE: the sources needs to be added in order from the old to the new </param>
        /// <param name="targetVersion"></param>
        /// <exception cref="NullReferenceException"></exception>
        /// <exception cref="Exception"></exception>
        public DefinitionConversionTarget(List<string> sourceVersions, string targetVersion)
        {
            if (sourceVersions is null)
            {
                throw new NullReferenceException("Source version cannot be null");
            }

            if (targetVersion is null)
            {
                throw new NullReferenceException("Target version cannot be null");
            }

            if (sourceVersions.Contains(targetVersion))
            {
                throw new Exception("Source version cannot be equal to the target version");
            }

            SourceVersions = sourceVersions;
            TargetVersion = targetVersion;
        }

        public override string ToString()
        {
            return $"Definition Conversion Target (Amount of Supported Versions: {SourceVersions.Count}, Target: {TargetVersion})";
        }
    }
}
