using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
    public interface ISkinColorController
    {
        ColorAsset CurrentColor { get; }

        event Action Updated;

        UniTask LoadAndSetSkinColorAsync(string assetId);

        void SetSkinColor(ColorAsset colorAsset);
        void SetSkinColor(Color color);
        bool IsColorEquipped(string assetId);
    }
}
