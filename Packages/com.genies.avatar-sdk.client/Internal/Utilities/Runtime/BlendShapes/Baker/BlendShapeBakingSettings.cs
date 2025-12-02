using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Toolbox.Core;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Serializable settings to expose a generic way to setup a blend shape baking config with regular expressions and
    /// other tools.
    /// </summary>
    [Serializable]
    public sealed class BlendShapeBakingSettings
    {
        [Tooltip("If enabled, the rest of the settings will be ignored and all blend shapes will be baked")]
        public bool justBakeAll;
        [TextArea, DisableIf(nameof(justBakeAll), true), Tooltip("Regular expression that defines what blend shapes will be baked (excluding merged ones)")]
        public string bakeRegex;
        [TextArea, DisableIf(nameof(justBakeAll), true), Tooltip("Regular expression that defines what blend shapes will be included (excluding merged and baked ones)")]
        public string includeRegex;
        [DisableIf(nameof(justBakeAll), true), Tooltip("Defines what blend shapes will be merged together")]
        public List<MergeGroup> mergeGroups = new();

        /// <summary>
        /// Gets the <see cref="BlendShapeBaker.Config"/> for the given mesh based on the current settings.
        /// </summary>
        public BlendShapeBaker.Config GetBakerConfig(Mesh mesh)
        {
            if (!mesh)
            {
                return default;
            }

            int blendShapeCount = mesh.blendShapeCount;
            var config = new BlendShapeBaker.Config
            {
                ShapesToBake = new List<int>(blendShapeCount),
                ShapesToMerge = new List<BlendShapeBaker.ShapeMerge>(blendShapeCount),
                ShapesToInclude = new List<int>(blendShapeCount),
            };

            if (justBakeAll)
            {
                for (int i = 0; i < blendShapeCount; ++i)
                {
                    config.ShapesToBake.Add(i);
                }

                return config;
            }

            // obtain shape merges
            int mergeCount = mergeGroups?.Count ?? 0;
            var mergedIndices = new HashSet<int>();
            for (int i = 0; i < mergeCount; ++i)
            {
                if (!TryGetShapeMerge(mesh, mergeGroups![i], out BlendShapeBaker.ShapeMerge shapeMerge))
                {
                    continue;
                }

                config.ShapesToMerge.Add(shapeMerge);
                mergedIndices.UnionWith(shapeMerge.Indices);
            }

            // obtain shape bakes and includes, excluding the shapes that will be merged
            Regex bakeEx = GetRegex(bakeRegex);
            Regex includeEx = GetRegex(includeRegex);
            for (int shapeIndex = 0; shapeIndex < blendShapeCount; ++shapeIndex)
            {
                if (mergedIndices.Contains(shapeIndex))
                {
                    continue;
                }

                string blendShapeName = mesh.GetBlendShapeName(shapeIndex);

                // bakes have higher precedence over includes
                if (bakeEx.IsMatch(blendShapeName))
                {
                    config.ShapesToBake.Add(shapeIndex);
                    continue;
                }

                if (includeEx.IsMatch(blendShapeName))
                {
                    config.ShapesToInclude.Add(shapeIndex);
                }
            }

            return config;
        }

        private static bool TryGetShapeMerge(Mesh mesh, MergeGroup mergeGroup, out BlendShapeBaker.ShapeMerge shapeMerge)
        {
            shapeMerge = default;
            if (string.IsNullOrEmpty(mergeGroup.outputName) || string.IsNullOrEmpty(mergeGroup.regex))
            {
                return false;
            }

            var indices = new List<int>(mesh.blendShapeCount);
            var regex = new Regex(mergeGroup.regex);

            for (int shapeIndex = 0; shapeIndex < mesh.blendShapeCount; ++shapeIndex)
            {
                if (regex.IsMatch(mesh.GetBlendShapeName(shapeIndex)))
                {
                    indices.Add(shapeIndex);
                }
            }

            if (indices.Count == 0)
            {
                return false;
            }

            shapeMerge = new BlendShapeBaker.ShapeMerge
            {
                Name = mergeGroup.outputName,
                Indices = indices,
            };

            return true;
        }

        private static Regex GetRegex(string regex)
        {
            return new Regex(string.IsNullOrEmpty(regex) ? "^$" : regex);
        }

        [Serializable]
        public struct MergeGroup
        {
            [Tooltip("The name for the final merged blend shape")]
            public string outputName;
            [Tooltip("Blend shape names matching this regular expression will be merged under this group")]
            public string regex;
        }
    }
}
