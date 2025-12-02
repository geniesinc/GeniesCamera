using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Manages deserialization and conversion of versioned Json definitions. Versioned definitions are json objects
    /// containing a string "Version" field. You can implement <see cref="IDefinitionConverter"/>s and pass them to the
    /// manager so it automatically handles conversion from older definition versions.
    /// </summary>
    public sealed class DefinitionManager : IDefinitionConverter
    {
        /// <summary>
        /// The default target version used when no target is specified for deserialization or conversion.
        /// </summary>
        public string DefaultVersion;
        public string TargetVersion => _targetVersion;
        private readonly Dictionary<DefinitionConversionTarget, IDefinitionConverter> _converters;
        private readonly DefinitionConversionPathFinder _conversionPathFinder;
        private readonly List<DefinitionConversionTarget> _conversionPath;
        private string _targetVersion;

        public DefinitionManager(string defaultVersion, IEnumerable<IDefinitionConverter> converters = null)
        {
            DefaultVersion = defaultVersion;

            _converters = new Dictionary<DefinitionConversionTarget, IDefinitionConverter>();

            if (converters is not null)
            {
                var targets = new HashSet<DefinitionConversionTarget>();

                foreach (IDefinitionConverter converter in converters)
                {
                    if (converter is null)
                    {
                        continue;
                    }

                    targets.Clear();
                    converter.GetSupportedConversionTargets(targets);

                    foreach (DefinitionConversionTarget target in targets)
                    {
                        if (!_converters.TryAdd(target, converter))
                        {
                            Debug.LogWarning($"Found more than one definition converter for the following conversion target: {target}");
                        }
                    }
                }
            }

            _conversionPathFinder = new DefinitionConversionPathFinder(_converters.Keys);
            _conversionPath = new List<DefinitionConversionTarget>(_conversionPathFinder.MaxPathLength);
        }

        public UniTask<T> DeserializeToDefaultAsync<T>(string definition, string versionKey = DefinitionToken.DefaultVersionKey)
        {
            return DeserializeAsync<T>(definition, DefaultVersion, versionKey);
        }

        public async UniTask<T> DeserializeAsync<T>(string definition, string targetVersion, string versionKey = DefinitionToken.DefaultVersionKey)
        {
            DefinitionToken definitionToken = DefinitionToken.Parse(definition, versionKey);
            definitionToken = await ConvertAsync(definitionToken, targetVersion);
            return definitionToken.Token.ToObject<T>();
        }

        public UniTask<string> ConvertToDefaultAsync(string definition, string versionKey = DefinitionToken.DefaultVersionKey)
        {
            return ConvertAsync(definition, DefaultVersion, versionKey);
        }

        public async UniTask<string> ConvertAsync(string definition, string targetVersion, string versionKey = DefinitionToken.DefaultVersionKey)
        {
            DefinitionToken definitionToken = DefinitionToken.Parse(definition, versionKey);

            if (definitionToken.Version == targetVersion)
            {
                return definition;
            }

            definitionToken = await ConvertAsync(definitionToken, targetVersion);
            return definitionToken.Token.ToString();
        }

        public UniTask<DefinitionToken> ConvertToDefaultAsync(DefinitionToken definitionToken)
        {
            return ConvertAsync(definitionToken, DefaultVersion);
        }

#region IDefinitionConverter


public void GetSupportedConversionTargets(ICollection<DefinitionConversionTarget> results)
        {
            if (results is null)
            {
                return;
            }

            foreach (DefinitionConversionTarget target in _conversionPathFinder.SupportedTargets)
            {
                results.Add(target);
            }
        }

        public bool SupportsConversionTarget(DefinitionConversionTarget target)
        {
            return _conversionPathFinder.HasConversionPath(target);
        }

        public async UniTask<DefinitionToken> ConvertAsync(DefinitionToken definitionToken, string targetVersion)
        {
            if (definitionToken.Version == targetVersion)
            {
                return definitionToken;
            }

            // try to find the shortest conversion path
            _conversionPath.Clear();
            if (!_conversionPathFinder.TryFindConversionPath(definitionToken.Version, targetVersion, _conversionPath))
            {
                throw new Exception($"[{nameof(DefinitionManager)}] couldn't find a definition conversion path from version {definitionToken.Version} to version {targetVersion}");
            }

            foreach (DefinitionConversionTarget conversionTarget in _conversionPath)
            {
                // try to get a converter from the current conversion target (this exception should never be thrown)
                if (!_converters.TryGetValue(conversionTarget, out IDefinitionConverter converter))
                {
                    throw new Exception($"[{nameof(DefinitionManager)}] no converter found to convert definition from version {definitionToken.Version} to version {targetVersion}");
                }

                definitionToken = await converter.ConvertAsync(definitionToken, targetVersion);

                // lets check just in case the definition converter is not properly implemented
                if (definitionToken.Version != conversionTarget.TargetVersion)
                {
                    throw new Exception($"[{nameof(DefinitionManager)}] definition converter implementation returned an unexpected definition token. Expected version {conversionTarget.TargetVersion} but returned {definitionToken.Version}");
                }
            }

            return definitionToken;
        }
#endregion
    }
}
