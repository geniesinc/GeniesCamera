using System;
using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains static methods to combine multiple materials together in a single one, procedurally generating texture
    /// atlases if required. No checks will be performed as it is assumed that all given materials are combinable.
    /// </summary>
    public static class MaterialCombiner
    {
        private static readonly AtlasLayoutBuilder AtlasLayoutBuilder = new();

        public static Result CombineMaterials(IList<Material> materials, TextureSettings textureSettings, bool combineTextures = true)
        {
            CombinableTextureProperty[] combinableTextureProperties = MaterialCombinerUtility.GetCombinableTextureProperties(materials[0].shader);

            var items = new List<Item>();
            foreach (Material material in materials)
            {
                var item = new Item
                {
                    Material = material,
                    TextureSize = MaterialCombinerUtility.GetCombinableTextureSize(material, combinableTextureProperties),
                };
                
                items.Add(item);
            }
            
            return CombineMaterials(items, textureSettings, combineTextures);
        }

        public static Result CombineMaterials(IEnumerable<Item> materials, TextureSettings textureSettings, bool combineTextures = true)
        {
            if (materials is not IList<Item> items)
            {
                items = new List<Item>(materials);
            }

            if (items.Count == 0)
            {
                return default;
            }

            if (!combineTextures)
            {
                return CreateNonCombinedResult(items);
            }

            // get combinable texture properties for this material and initialize the result struct
            CombinableTextureProperty[] combinableTextureProperties = MaterialCombinerUtility.GetCombinableTextureProperties(items[0].Material.shader);
            var combinedTextures = new Texture[combinableTextureProperties.Length];
            var result = new Result(items.Count, combinedTextures)
            {
                Material = new Material(items[0].Material)
            };
            
            // build the atlas layout into the result arrays
            var maxAtlasSize = new Vector2Int(textureSettings.width, textureSettings.height);
            BuildAtlasLayout(result, items, maxAtlasSize);
            
            // render combined textures
            for (int i = 0; i < combinableTextureProperties.Length; ++i)
            {
                CombinableTextureProperty property = combinableTextureProperties[i];
                Texture texture = RenderCombinedTexture(property.Id, property.DefaultColor, items, textureSettings, result.AtlasPositions, result.AtlasSizes);
                if (!texture)
                {
                    continue;
                }

                combinedTextures[i] = texture;
                texture.name = $"{property.Name}__Combined";
                result.Material.SetTexture(property.Id, texture);
            }
            
            return result;
        }
        
        private static Result CreateNonCombinedResult(IList<Item> items)
        {
            var result = new Result(items.Count)
            {
                Material = new Material(items[0].Material)
            };
            
            for (int i = 0; i < items.Count; ++i)
            {
                result.UvOffsets[i] = Vector2.zero;
                result.UvScales[i]  = Vector2.one;
            }
            
            return result;
        }

        private static void BuildAtlasLayout(Result result, IList<Item> items, Vector2Int maxAtlasSize)
        {
            // build atlas layout
            AtlasLayoutBuilder.BeginBuild(maxAtlasSize.x, maxAtlasSize.y);
            foreach (Item item in items)
            {
                AtlasLayoutBuilder.AddRect(item.TextureSize.x, item.TextureSize.y);
            }

            AtlasLayoutBuilder.EndBuild();
            
            // populate result with the packed item rects
            Vector2 atlasSize = AtlasLayoutBuilder.AtlasSize;
            for (int i = 0; i < items.Count; ++i)
            {
                result.AtlasPositions[i] = AtlasLayoutBuilder.GetPackedRectPosition(i);
                result.AtlasSizes[i]     = AtlasLayoutBuilder.GetPackedRectSize(i);
                result.UvOffsets[i]      = result.AtlasPositions[i] / atlasSize;
                result.UvScales[i]       = result.AtlasSizes[i] / atlasSize;
            }
        }

        private static Texture RenderCombinedTexture(
            int propertyId, Color defaultColor,
            IList<Item> items, TextureSettings textureSettings,
            Vector2Int[] positions, Vector2Int[] sizes)
        {
            TextureSettingsRenderer renderer = default;
            
            for (int i = 0; i < items.Count; ++i)
            {
                Texture source = items[i].Material.GetTexture(propertyId);
                if (!source)
                {
                    continue;
                }

                if (!renderer.IsRendering)
                {
                    renderer = textureSettings.BeginRendering(AtlasLayoutBuilder.AtlasSize);
                    renderer.RenderTexture.WriteColor(defaultColor);
                }
                
                Vector2Int targetMin = positions[i];
                Vector2Int targetMax = targetMin + sizes[i];
                TextureBlitter.BlitIntoUvRect(source, renderer.RenderTexture, targetMin, targetMax);
            }
            
            return renderer.IsRendering ? renderer.FinishRendering() : null;
        }

        public struct Item
        {
            public Material   Material;
            public Vector2Int TextureSize;
        }
        
        public struct Result : IDisposable
        {
            public          Material     Material;
            public readonly Vector2[]    UvOffsets;
            public readonly Vector2[]    UvScales;
            public readonly Vector2Int[] AtlasPositions;
            public readonly Vector2Int[] AtlasSizes;
            
            private readonly Texture[] _combinedTextures;

            public Result(int materialCount, Texture[] combinedTextures = null)
            {
                Material          = null;
                UvOffsets         = new Vector2[materialCount];
                UvScales          = new Vector2[materialCount];
                AtlasPositions     = new Vector2Int[materialCount];
                AtlasSizes         = new Vector2Int[materialCount];
                _combinedTextures = combinedTextures;
            }

            public void Dispose()
            {
                if (Material)
                {
                    Object.Destroy(Material);
                }

                Material = null;
                
                if (_combinedTextures is null)
                {
                    return;
                }

                for (int i = 0; i < _combinedTextures.Length; ++i)
                {
                    if (_combinedTextures[i])
                    {
                        Object.Destroy(_combinedTextures[i]);
                    }

                    _combinedTextures[i] = null;
                }
            }
        }
    }
}