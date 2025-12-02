using System;
using System.Collections.Generic;
using System.Linq;
using Genies.Models;
using UMA;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the resources for setting up a Genie from a provided GAP avatar mesh
    /// TODO: Need to remove UnityEditor dependency!
    /// </summary>
    public sealed class SubSpeciesAsset : IAsset, IDisposable
    {
	    public string Species => GenieSpecies.UnifiedGAP;
        public string Id { get; }
        public string Lod { get; }
        public bool IsDisposed { get; private set; }

        public readonly RaceData.UMATarget Target = RaceData.UMATarget.Humanoid;
        public readonly SkinnedMeshRenderer[] Renderers;
        public readonly GenieComponentAsset[] Components;
        public readonly HumanDescription HumanDescription;
        public readonly Mesh UtilityMesh;
        public string[] BlendshapeNames {get; private set; }
        public List<MeshAsset> MeshAssets {get; private set; }

        public SubSpeciesAsset(string id, string lod)
        {
	        throw new NotImplementedException($"[{nameof(SubSpeciesAsset)}] Getting asset from Id not yet implemented; " +
	                                          $"waiting for content management!");
        }

        public SubSpeciesAsset(SubSpeciesContainer container, string lod)
        {
	        Id = container.Id;
	        Lod = lod;
	        Renderers = container.BodyPrefab.GetComponentsInChildren<SkinnedMeshRenderer>();
	        HumanDescription = container.Avatar.humanDescription;
	        Components = container.Components.Cast<GenieComponentAsset>().ToArray();
	        UtilityMesh = container.UtilityMesh;

	        PopulateMeshAssets();
	        PopulateBlendshapeNames();
        }

        private void PopulateMeshAssets()
        {
	        SkeletonBone[] hdSkeleton = HumanDescription.skeleton;
	        Dictionary<string, SkeletonBone> boneTransforms = new Dictionary<string, SkeletonBone>();
	        foreach (var bone in hdSkeleton)
            {
                boneTransforms.Add(bone.name, bone);
            }

            MeshAssets = new List<MeshAsset>();
            foreach (SkinnedMeshRenderer smr in Renderers)
            {
                MeshAsset ma = CreateMeshAssetFrom(smr, boneTransforms);
                ma.NoMerge = true;
                ma.NoTextureCombine = false;
                MeshAssets.Add(ma);
            }
        }

        private MeshAsset CreateMeshAssetFrom(SkinnedMeshRenderer smr, Dictionary<string, SkeletonBone> boneTransforms)
        {
            if (smr.sharedMaterials.Length > 1)
            {
                Debug.LogWarning($"{smr.gameObject.name} has more than one material assigned");
            }

            var bones = smr.bones;
            List<UMATransform> umaBones = new List<UMATransform>();
            int[] boneNameHashes = new int[smr.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
	            bones[i].localPosition = boneTransforms[bones[i].name].position;
	            bones[i].localRotation = boneTransforms[bones[i].name].rotation;
	            bones[i].localScale = boneTransforms[bones[i].name].scale;
	            int boneHash = Animator.StringToHash(bones[i].name);
	            int parentHash = Animator.StringToHash(bones[i].parent.name);
	            UMATransform umaBone = new UMATransform(bones[i],
		            boneHash,
		            parentHash);
	            umaBones.Add(umaBone);
	            boneNameHashes[i] = boneHash;
            }
            umaBones.Sort(UMATransform.TransformComparer);
            smr.bones = bones;

            Mesh mesh = smr.sharedMesh;
            UMAMeshData meshData = new UMAMeshData();
            meshData.RetrieveDataFromUnityMesh(mesh);
            meshData.boneNameHashes = boneNameHashes;

            // Create the bindpose data array as that is not coming directly from UMA
            var bindposes = new BindposeData[meshData.bindPoses.Length];
            for (int i = 0; i < bindposes.Length; ++i)
            {
	            bindposes[i] = new BindposeData
	            {
		            BoneHash = boneNameHashes[i],
		            Matrix = meshData.bindPoses[i],
	            };
            }

            var asset = new MeshAsset
            {
                Id             = mesh.name,
                Material       = smr.sharedMaterial,
                Indices        = mesh.triangles,
                Vertices       = meshData.vertices,
                Normals        = meshData.normals,
                Tangents       = meshData.tangents,
                Uvs            = meshData.uv,
                Bones          = umaBones.ToArray(),
                Bindposes      = bindposes,
                BonesPerVertex = meshData.ManagedBonesPerVertex,
                BoneWeights    = meshData.ManagedBoneWeights,
                BlendShapes    = meshData.blendShapes,
            };

            asset.ScheduleSmpsuCalculation();
            return asset;
        }

        private void PopulateBlendshapeNames()
        {
            // Gather all blendshape names
            List<string> blendshapeNames = new List<string>();
            foreach (SkinnedMeshRenderer smr in Renderers)
            {
                Mesh mesh = smr.sharedMesh;
                int blendShapeCount = mesh.blendShapeCount;

                for (int i = 0; i < blendShapeCount; i++)
                {
                    string blendShapeName = mesh.GetBlendShapeName(i);
                    if (!blendshapeNames.Contains(blendShapeName))
                    {
                        blendshapeNames.Add(blendShapeName);
                    }
                }
            }

            BlendshapeNames = blendshapeNames.ToArray();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
        }
    }
}
