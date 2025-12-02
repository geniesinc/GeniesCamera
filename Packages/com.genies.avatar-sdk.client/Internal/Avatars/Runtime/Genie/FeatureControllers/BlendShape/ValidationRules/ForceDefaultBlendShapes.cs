using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Given a default set of blend shapes mapped by the slot, it will ensure that the given assets includes the default items
    /// for the empty slots that have one.
    /// </summary>
    public sealed class ForceDefaultBlendShapes : IAssetsValidationRule<BlendShapeAsset>
    {
        // state
        private readonly Dictionary<string, BlendShapeAsset> _defaultAssetsBySlot;

        public ForceDefaultBlendShapes(IEnumerable<BlendShapeAsset> blendShapes = null)
        {
            _defaultAssetsBySlot = new Dictionary<string, BlendShapeAsset>();
            SetDefaultBlendShapes(blendShapes);
        }

        public void Apply(HashSet<BlendShapeAsset> assets)
        {
            foreach (KeyValuePair<string, BlendShapeAsset> keyValuePair in _defaultAssetsBySlot)
            {
                // if the slot is empty, add the default blend shape
                if(!assets.Any(asset => asset.Slot == keyValuePair.Key))
                {
                    assets.Add(keyValuePair.Value);
                }
            }
        }

        public void SetDefaultBlendShapes(IEnumerable<BlendShapeAsset> blendShapes)
        {
            _defaultAssetsBySlot.Clear();
            if (blendShapes is null)
            {
                return;
            }

            foreach (BlendShapeAsset asset in blendShapes)
            {
                if (asset == null)
                {
                    Debug.LogWarning("ForceDefaultBlendShapes's SetDefaultBlendShapes got a null default BlendShapeAsset. Skipping.");
                    continue;
                }

                _defaultAssetsBySlot[asset.Slot] = asset;
            }
        }
    }
}
