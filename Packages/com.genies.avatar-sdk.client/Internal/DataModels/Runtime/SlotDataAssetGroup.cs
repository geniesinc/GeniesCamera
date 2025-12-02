using System;
using System.Collections.Generic;
using UMA;

namespace Genies.Models
{
    [Serializable]
    public struct SlotDataAssetGroup
    {
        public string              id;
        public List<SlotDataAsset> slots;
    }
}
