using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    public static class MeshAssetUtility
    {
        public static List<MeshAsset> CreateMeshAssetsFrom(UMATextRecipe recipe)
        {
            var results = new List<MeshAsset>();
            CreateMeshAssetsFrom(recipe, results);
            return results;
        }

        public static void CreateMeshAssetsFrom(UMATextRecipe recipe, ICollection<MeshAsset> results)
        {
            SlotData[] slots = recipe.GetUMARecipe().slotDataList;
            foreach (SlotData slot in slots)
            {
                if (slot is not null)
                {
                    results.Add(CreateMeshAssetFrom(slot));
                }
            }
        }

        public static List<MeshAsset> CreateMeshAssetsFrom(IEnumerable<SlotData> slots)
        {
            var results = new List<MeshAsset>();
            foreach (SlotData slot in slots)
            {
                if (slot is not null)
                {
                    results.Add(CreateMeshAssetFrom(slot));
                }
            }

            return results;
        }

        public static List<MeshAsset> CreateMeshAssetsFrom(IEnumerable<SpeciesAsset> speciesAssets)
        {
            var results = new List<MeshAsset>();
            foreach (SpeciesAsset speciesAsset in speciesAssets)
            {
                CreateMeshAssetsFrom(speciesAsset, results);
            }

            return results;
        }

        public static List<MeshAsset> CreateMeshAssetsFrom(SpeciesAsset asset)
        {
            var results = new List<MeshAsset>(asset.Slots.Length);
            CreateMeshAssetsFrom(asset, results);
            return results;
        }

        public static void CreateMeshAssetsFrom(SpeciesAsset asset, ICollection<MeshAsset> results)
        {
            if (asset is null || asset.Race.baseRaceRecipe is not UMATextRecipe recipe)
            {
                return;
            }

            Dictionary<string, string> mapping = GetSlotOverlayMappingFromRecipeText(recipe.recipeString);
            foreach (SlotDataAsset slot in asset.Slots)
            {
                if (!slot || !mapping.TryGetValue(slot.name, out string overlayName))
                {
                    continue;
                }

                OverlayDataAsset overlay = null;
                foreach (OverlayDataAsset overlayAsset in asset.Overlays)
                {
                    if (!overlayAsset || overlayAsset.name != overlayName)
                    {
                        continue;
                    }

                    overlay = overlayAsset;
                    break;
                }

                if (overlay)
                {
                    results.Add(CreateMeshAssetFrom(slot, overlay));
                }
            }
        }

        public static List<MeshAsset> CreateMeshAssetsFrom(IEnumerable<OutfitAsset> outfitAssets)
        {
            var results = new List<MeshAsset>();
            foreach (OutfitAsset outfitAsset in outfitAssets)
            {
                CreateMeshAssetsFrom(outfitAsset, results);
            }

            return results;
        }

        public static List<MeshAsset> CreateMeshAssetsFrom(OutfitAsset asset)
        {
            var results = new List<MeshAsset>(asset.Slots.Length);
            CreateMeshAssetsFrom(asset, results);
            return results;
        }

        public static void CreateMeshAssetsFrom(OutfitAsset asset, ICollection<MeshAsset> results)
        {
            if (asset is null)
            {
                return;
            }

            for (int i = 0; i < asset.Slots.Length; ++i)
            {
                results.Add(CreateMeshAssetFrom(asset.Slots[i], asset.Overlays[i]));
            }
        }

        public static MeshAsset CreateMeshAssetFrom(SlotData slot)
        {
            return CreateMeshAssetFrom(slot.asset, slot.GetOverlay(0).asset);
        }

        public static MeshAsset CreateMeshAssetFrom(SlotDataAsset slot, OverlayDataAsset overlay)
        {
            Material material = CreateMaterialFrom(overlay, out bool noTextureCombine);
            MeshAsset meshAsset = CreateMeshAssetFrom(slot, material);
            meshAsset.NoTextureCombine = noTextureCombine;

            return meshAsset;
        }

        public static Material CreateMaterialFrom(OverlayDataAsset overlay, out bool noTextureCombine)
        {
            return CreateMaterialFrom(overlay.material, overlay.textureList, out noTextureCombine);
        }

        public static Material CreateMaterialFrom(UMAMaterial umaMaterial, IList<Texture> textureList, out bool noTextureCombine)
        {
            noTextureCombine = umaMaterial.materialType is UMAMaterial.MaterialType.NoAtlas;
            var material = new Material(umaMaterial.material)
            {
                name = umaMaterial.name
            };

            if (umaMaterial.materialType is UMAMaterial.MaterialType.UseExistingTexture)
            {
                return material;
            }

            for (int i = 0; i < umaMaterial.channels.Length; ++i)
            {
                UMAMaterial.MaterialChannel channel = umaMaterial.channels[i];
                Texture texture = textureList[i];
                material.SetTexture(channel.materialPropertyName, texture);
            }

            return material;
        }

        public static MeshAsset CreateMeshAssetFrom(SlotDataAsset slot, Material material)
        {
            UMAMeshData meshData = slot.meshData;

            // create the bindpose data array as that is not coming directly from UMA
            var bindposes = new BindposeData[meshData.bindPoses.Length];
            for (int i = 0; i < bindposes.Length; ++i)
            {
                bindposes[i] = new BindposeData
                {
                    BoneHash = meshData.boneNameHashes[i],
                    Matrix   = meshData.bindPoses[i],
                };
            }

            var asset = new MeshAsset
            {
                Id = slot.name,
                Material = material,
                Indices = meshData.submeshes[0].triangles,
                Vertices = meshData.vertices,
                Normals = meshData.normals,
                Tangents = meshData.tangents,
                Uvs = meshData.uv,
                Bones = meshData.umaBones,
                Bindposes = bindposes,
                BonesPerVertex = meshData.ManagedBonesPerVertex,
                BoneWeights = meshData.ManagedBoneWeights,
                BlendShapes = meshData.blendShapes
            };

            asset.ScheduleSmpsuCalculation();
            return asset;
        }

        public static List<MeshAssetTriangleFlags> CreateTriangleFlagsFrom(IEnumerable<MeshHideAsset> assets)
        {
            var results = new List<MeshAssetTriangleFlags>();
            CreateTriangleFlagsFrom(assets, results);
            return results;
        }

        public static void CreateTriangleFlagsFrom(IEnumerable<MeshHideAsset> assets, ICollection<MeshAssetTriangleFlags> results)
        {
            foreach (MeshHideAsset asset in assets)
            {
                if (asset)
                {
                    results.Add(CreateTriangleFlagsFrom(asset));
                }
            }
        }

        public static MeshAssetTriangleFlags CreateTriangleFlagsFrom(MeshHideAsset asset)
        {
            // our non-uma system is designed so each MeshAsset is one submesh, unlike UMA that supports multiple submeshes pero slot, but we don't use that on our content
            return new MeshAssetTriangleFlags
            {
                Id                = asset.name,
                TargetMeshAssetId = asset.AssetSlotName,
                Triangles         = asset.triangleFlags[0],
            };
        }

        public static Dictionary<string, string> GetSlotOverlayMappingFromRecipeText(string recipeText)
        {
            var recipe = JsonConvert.DeserializeObject<AnnoyingUmaRecipe>(recipeText);
            var mapping = new Dictionary<string, string>();

            foreach (SlotV3 slot in recipe.slotsV3)
            {
                if (string.IsNullOrEmpty(slot.id) || (slot.copyIdx < 0 && slot.overlays?.Count == 0))
                {
                    continue;
                }

                if (slot.copyIdx < 0)
                {
                    mapping[slot.id] = slot.overlays[0].id;
                }
                else
                {
                    mapping[slot.id] = recipe.slotsV3[slot.copyIdx].overlays[0].id;
                }
            }

            return mapping;
        }

        [Serializable]
        private struct AnnoyingUmaRecipe
        {
            public List<SlotV3> slotsV3;
        }

        [Serializable]
        private struct SlotV3
        {
            public string id;
            public int copyIdx;
            public List<Overlay> overlays;
        }

        [Serializable]
        private struct Overlay
        {
            public string id;
        }
    }
}
