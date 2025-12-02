using System;
using UnityEngine;
using Toolbox.Core;

namespace Genies.APIResolver
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "APIResolverData", menuName = "Genies/APIResolverData")]
#endif
    public class APIResolverData : ScriptableObject
    {
        [Header("Active Party")]
        public Party _activeParty;

        [Header("Party Configurations")]
        public SerializedDictionary<Party, PartyData> _partyData;

        public string GetPartyId(string bundleId)
        {
            if (_partyData != null &&
                _partyData.TryGetValue(_activeParty, out var data) &&
                data._bundleIdToPartyId != null &&
                data._bundleIdToPartyId.TryGetValue(bundleId, out var partyId))
            {
                return partyId;
            }

            Debug.LogWarning($"No PartyId found for bundleId '{bundleId}' under active party '{_activeParty}'");
            return null;
        }
    }

    public enum Party
    {
        GeniesParty,
    }

    [Serializable]
    public class PartyData
    {
        public SerializedDictionary<string, string> _bundleIdToPartyId;
    }
}
