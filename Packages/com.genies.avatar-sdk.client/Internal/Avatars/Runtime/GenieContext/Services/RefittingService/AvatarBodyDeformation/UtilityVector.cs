using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed class UtilityVector
    {
        public readonly string Name;
        public readonly string Version;
        public readonly IReadOnlyList<UtilMesh> UtilMeshes;

        public UtilityVector(string name, string version, IReadOnlyList<UtilMesh> utilMeshes)
        {
            Name = name;
            Version = version;
            UtilMeshes = utilMeshes;
        }
    }

    public sealed class UtilMesh
    {
        public readonly UtilMeshName Name;
        public readonly IReadOnlyList<UtilMeshRegion> Regions;

        public UtilMesh(UtilMeshName name, IReadOnlyList<UtilMeshRegion> regions)
        {
            Name = name;
            Regions = regions;
        }
    }

    public sealed class UtilMeshRegion
    {
        public readonly RegionType Region;
        public readonly Vector3[] UniquePoints;

        public UtilMeshRegion(RegionType region, Vector3[] uniquePoints)
        {
            Region = region;
            UniquePoints = uniquePoints;
        }
    }

    public enum UtilMeshName
    {
        bodysuit,
        dress,
        outerwear,
        pants,
        scalp,
        shirt,
        shoes,
        skirt,
        none
    }

    public enum RegionType
    {
        wholeTarget,
        biceps,
        calves,
        chest,
        forearms,
        hands,
        head,
        hips,
        neck,
        shoulders,
        thighs,
        waist
    }
}
