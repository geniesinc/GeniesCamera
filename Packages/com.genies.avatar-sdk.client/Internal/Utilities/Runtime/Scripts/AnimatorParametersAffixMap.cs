using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Utility to quickly map animator parameters by a group of prefixes/suffixes.
    /// </summary>
    public sealed class AnimatorParametersAffixMap : IEnumerable<KeyValuePair<string, AnimatorParametersAffixMap.AffixedParameters>>
    {
        public IReadOnlyList<string> Ids { get; }
        public AffixType Type { get; private set; }

        private readonly List<string> _ids; // redundant collection of ids to maintain the order in which they were found within the parameters
        private readonly Dictionary<string, AffixedParameters> _map;

        public AnimatorParametersAffixMap()
        {
            _ids = new List<string>();
            _map = new Dictionary<string, AffixedParameters>();
            Ids = _ids.AsReadOnly();
        }

        public AnimatorParametersAffixMap(AffixType type, AnimatorParameters parameters, IEnumerable<string> affixes)
            : this()
        {
            MapParameters(type, parameters, affixes);
        }

        public void MapParameters(AffixType type, AnimatorParameters parameters, IEnumerable<string> affixes)
        {
            Type = type;
            _ids.Clear();
            _map.Clear();

            var affixedParametersById = new Dictionary<string, Dictionary<string, AnimatorControllerParameter>>();
            TryGetIdAndAffixDelegate tryGetIdAndAffix = type switch
            {
                AffixType.Prefix => TryGetIdAndPrefix,
                AffixType.Suffix => TryGetIdAndSuffix,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            // ensure non-duplicate affixes as well as preventing multiple enumeration issues with the tryGetIdAndAffix callback
            var affixesSet = new HashSet<string>(affixes);

            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (!tryGetIdAndAffix(parameter.name, affixesSet, out string id, out string affix))
                {
                    continue;
                }

                if (!affixedParametersById.TryGetValue(id, out Dictionary<string, AnimatorControllerParameter> parametersByAffix))
                {
                    parametersByAffix = new Dictionary<string, AnimatorControllerParameter>();
                    affixedParametersById.Add(id, parametersByAffix);
                    _ids.Add(id);
                    _map.Add(id, new AffixedParameters(id, type, parametersByAffix));
                }

                parametersByAffix[affix] = parameter;
            }
        }

        public bool ContainsId(string id)
            => _map.ContainsKey(id);

        public bool TryGetAffixedParameters(string id, out AffixedParameters parameters)
            => _map.TryGetValue(id, out parameters);

        public IEnumerator<KeyValuePair<string, AffixedParameters>> GetEnumerator()
            => (_map as IEnumerable<KeyValuePair<string, AffixedParameters>>).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private static readonly TryGetIdAndAffixDelegate TryGetIdAndPrefix = (string name, IEnumerable<string> prefixes, out string id, out string prefix) =>
        {
            foreach (string lookingPrefix in prefixes)
            {
                if (!name.StartsWith(lookingPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                id = name[lookingPrefix.Length..];
                prefix = lookingPrefix;
                return true;
            }

            id = null;
            prefix = null;
            return false;
        };

        private static readonly TryGetIdAndAffixDelegate TryGetIdAndSuffix = (string name, IEnumerable<string> suffixes, out string id, out string suffix) =>
        {
            foreach (string lookingSuffix in suffixes)
            {
                if (!name.EndsWith(lookingSuffix, StringComparison.Ordinal))
                {
                    continue;
                }

                int substringIndex = name.Length - lookingSuffix.Length;
                id = name[..substringIndex];
                suffix = lookingSuffix;
                return true;
            }

            id = null;
            suffix = null;
            return false;
        };

        public enum AffixType
        {
            Prefix = 0,
            Suffix = 1,
        }

        public readonly struct AffixedParameters : IEnumerable<KeyValuePair<string, AnimatorControllerParameter>>
        {
            public string Id => _id;
            public AffixType Type => _type;
            public IReadOnlyCollection<string> Affixes => _parametersByAffix.Keys;

            private readonly string _id;
            private readonly AffixType _type;
            private readonly Dictionary<string, AnimatorControllerParameter> _parametersByAffix;

            public AffixedParameters(string id, AffixType type, Dictionary<string, AnimatorControllerParameter> parametersByAffix)
            {
                _id = id;
                _type = type;
                _parametersByAffix = parametersByAffix;
            }

            public bool ContainsAffix(string affix)
                => _parametersByAffix.ContainsKey(affix);

            public bool TryGetParameter(string affix, out AnimatorControllerParameter parameter)
                => _parametersByAffix.TryGetValue(affix, out parameter);

            public IEnumerator<KeyValuePair<string, AnimatorControllerParameter>> GetEnumerator()
                => (_parametersByAffix as IEnumerable<KeyValuePair<string, AnimatorControllerParameter>>).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }

        private delegate bool TryGetIdAndAffixDelegate(string name, IEnumerable<string> affixes, out string id, out string affix);
    }
}
