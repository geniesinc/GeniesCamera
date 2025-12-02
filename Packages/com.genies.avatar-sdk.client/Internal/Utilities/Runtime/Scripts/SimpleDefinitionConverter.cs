using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    /// <summary>
    /// Simple converter that you can extend for single target conversions and avoid some boiler plate code for the
    /// <see cref="IDefinitionConverter"/> interface.
    /// </summary>
    public abstract class SimpleDefinitionConverter : IDefinitionConverter
    {
        protected readonly List<string> SourceVersions;
        private string _targetVersion;
        public string TargetVersion => _targetVersion;

        public SimpleDefinitionConverter(List<string> sourceVersions, string targetVersion)
        {
            SourceVersions = sourceVersions;
            _targetVersion = targetVersion;
        }

        protected abstract UniTask<DefinitionToken> ConvertAsync(DefinitionToken definition);

        public void GetSupportedConversionTargets(ICollection<DefinitionConversionTarget> results)
        {
            results?.Add(new DefinitionConversionTarget(SourceVersions, TargetVersion));
        }

        public bool SupportsConversionTarget(DefinitionConversionTarget target)
        {
            var hasSupportedVersion = SourceVersions.Any(t => target.SourceVersions.Any(s => s.Contains(t)));
            return hasSupportedVersion && TargetVersion == target.TargetVersion;
        }

        public UniTask<DefinitionToken> ConvertAsync(DefinitionToken definition, string targetVersion)
        {
            if (!SourceVersions.Contains(definition.Version))
            {
                throw new Exception($"This converter does not support conversion for the given definition: Definition version: {definition.Version} | Unsupported version: {definition.Version}");
            }

            if (targetVersion != TargetVersion)
            {
                throw new Exception($"This converter does not support conversion for the given target version: Given version: {targetVersion} | Supported version: {TargetVersion}");
            }

            return ConvertAsync(definition);
        }
    }

}
