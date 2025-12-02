using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    [Serializable]
    public struct GearAssetConfig
    {
        [FormerlySerializedAs("assetAddress")] public string AssetAddress;
        [FormerlySerializedAs("elementAddresses")] public List<string> ElementAddresses;

        public string GetUniqueName()
        {
            return $"{AssetAddress}_{string.Join("-", ElementAddresses)}";
        }
    }
}
