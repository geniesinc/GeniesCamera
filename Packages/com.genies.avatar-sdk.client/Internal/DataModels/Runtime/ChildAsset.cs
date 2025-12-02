using System;

namespace Genies.Models
{
    [Serializable]
    public class ChildAsset
    {
        public string guid;
        public string assetType;
        public ProtocolTag protocolTag;
    }
}