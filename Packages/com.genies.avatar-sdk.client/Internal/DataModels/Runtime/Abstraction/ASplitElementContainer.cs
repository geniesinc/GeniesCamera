using UnityEngine;

namespace Genies.Models
{
    public class ASplitElementContainer : OrderedScriptableObject
    {
        [SerializeField] private string assetId;
        [SerializeField] private string assetName;

        public string AssetId
        {
            get => assetId;
            set
            {
                assetName = value.Split('_')[1];
                assetId = value;
            }
        }
        
        public string AssetName
        {
            get => assetName;
            set => assetName = value;
        }
    }
}