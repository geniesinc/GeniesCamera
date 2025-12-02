using UMA;
using UnityEngine;
using System;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a single mesh asset (no sub-meshes) with a single material instance. It is assumed that the data is
    /// static and never changes, except for the <see cref="NoMerge"/> and <see cref="NoTextureCombine"/> fields.
    /// </summary>
    public sealed class MeshAsset
    {
        /// <summary>
        /// If enabled, <see cref="MeshBuilder"/> will create a separated submesh for this mesh asset. If disabled, the
        /// submesh for this asset may have other combinable assets merged.
        /// </summary>
        public bool NoMerge;

        /// <summary>
        /// If enabled, <see cref="MeshBuilder"/> will create a submesh for this asset that can merge with other
        /// combinable assets only if no texture combination (atlas) is required.
        /// <br/><br/>
        /// If <see cref="NoMerge"/> is enabled this property has no effect.
        /// </summary>
        public bool NoTextureCombine;

        // mesh asset ID, should be unique among the other assets
        public string Id;

        // mesh material
        public Material Material;

        // index buffer
        public int[] Indices;

        // vertex buffers
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Vector2[] Uvs;
        public byte[]    BonesPerVertex;

        // skinning
        public UMATransform[] Bones;
        public BindposeData[] Bindposes;
        public BoneWeight1[]  BoneWeights;

        // blend shapes
        public UMABlendShape[] BlendShapes;

        // quick solution for refitting issues with GAP avatars
        internal bool IsRefitted;

        // SquareMetersPerSquareUvs
        private MeshAssetSmpsuJob _smpsuJob;

        /// <summary>
        /// It is recommended to call this method early if you know that you will need the SMPSU calculation later.
        /// Calculation is done using the Unity jobs system, so it doesn't use the main thread. Call this method any
        /// time you change the data and want the SMPSU to be calculated again.
        /// </summary>
        public void ScheduleSmpsuCalculation()
        {
            _smpsuJob = new MeshAssetSmpsuJob(this);
        }

        /// <summary>
        /// Gets the SMPSU value for this asset. If the calculation was not scheduled before using
        /// <see cref="ScheduleSmpsuCalculation"/> it will be calculated and returned.
        /// </summary>
        public float GetSquareMetersPerSquareUvs()
        {
            _smpsuJob ??= new MeshAssetSmpsuJob(this);
            return _smpsuJob.GetSquareMetersPerSquareUvs();
        }
    }
}
