using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    /// <summary>
    /// <see cref="IUgcOutfitAssetBuilder"/> implementation that uses <see cref="LodUgcOutfitAssetBuilder"/> to implement
    /// multiple LODs
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "UgcOutfitAssetBuilder", menuName = "Genies/UGC OutfitAsset Builder")]
#endif
    public sealed class UgcOutfitAssetBuilder : ScriptableObject, IUgcOutfitAssetBuilder
    {
        [FormerlySerializedAs("defaultBuilder")] public LodUgcOutfitAssetBuilder DefaultBuilder;
        [FormerlySerializedAs("lodBuilders")] public List<LodUgcOutfitAssetBuilder> LodBuilders = new();

        public void Initialize(
            IAssetLoader<UgcTemplateAsset> templateLoader,
            IAssetLoader<UgcElementAsset> elementLoader,
            IMegaMaterialBuilder megaMaterialBuilder)
        {
            LodBuilders ??= new List<LodUgcOutfitAssetBuilder>();
            foreach (LodUgcOutfitAssetBuilder builder in LodBuilders)
            {
                builder.Initialize(templateLoader, elementLoader, megaMaterialBuilder);
            }
        }

        public UniTask<OutfitAsset> BuildOutfitAssetAsync(string wearableId, Wearable wearable, OutfitAssetMetadata metadata,
            string lod = AssetLod.Default)
        {
            if (TryGetBuilder(lod, out LodUgcOutfitAssetBuilder builder))
            {
                return builder.BuildOutfitAssetAsync(wearableId, wearable, metadata);
            }

            return UniTask.FromResult<OutfitAsset>(null);
        }

        public bool TryGetBuilder(string lod, out LodUgcOutfitAssetBuilder builder)
        {
            foreach (LodUgcOutfitAssetBuilder lodBuilder in LodBuilders)
            {
                if (!lodBuilder || lodBuilder.lod != lod)
                {
                    continue;
                }

                builder = lodBuilder;
                return true;
            }

            if (DefaultBuilder)
            {
                builder = DefaultBuilder;
                return true;
            }

            builder = null;
            return false;
        }
    }
}
