using System.Runtime.InteropServices;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Genies.Avatars
{
    public sealed class DistanceComputeDispatcher
    {
        private const int VertBufferStride = sizeof(float) * 3;
        private const int DistBufferStride = sizeof(float);

        private readonly ComputeShader _computeShader;

        public DistanceComputeDispatcher()
        {
            _computeShader = (ComputeShader)Resources.Load("DistanceComputeShader");

            if (!_computeShader)
            {
                Debug.LogError($"[{nameof(DistanceComputeDispatcher)}] DistanceComputeShader did not load.");
            }
        }

        // Compute the distances between all the points in setA and each point in setB O(n^2)
        public bool TryComputeDistanceMatrixGPU(Vector3[] setA, Vector3[] setB, out Matrix<float> distanceMatrix, float gamma = 1.0f)
        {
            distanceMatrix = null;
            if (setA == null || setB == null)
            {
                return false;
            }

            int numPtsA = setA.Length;
            int numPtsB = setB.Length;
            if (numPtsA <= 0 || numPtsB <= 0)
            {
                return false;
            }

            float[] distances = new float[setA.Length * setB.Length];

            // A GraphicsBuffer is a better version of a ComputeBuffer
            GraphicsBuffer sourceVertBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, setA.Length, VertBufferStride);
            GraphicsBuffer targetVertBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, setB.Length, VertBufferStride);
            GraphicsBuffer distBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, distances.Length, DistBufferStride);

            // Cache the kernel ID
            int kernel = _computeShader.FindKernel("CSMain");
            if (kernel < 0) // docs don't indicate failure ret val, just guessing here
            {
                return false;
            }

            // Set buffers and variables
            _computeShader.SetBuffer(kernel, "_SourceVertices", sourceVertBuffer);
            _computeShader.SetBuffer(kernel, "_TargetVertices", targetVertBuffer);
            _computeShader.SetBuffer(kernel, "_OutDistances", distBuffer);

            // Set data in the buffers
            _computeShader.SetInt("_SrcLen", numPtsA);
            _computeShader.SetInt("_TargetLen", numPtsB);
            _computeShader.SetFloat("_gamma", gamma);
            sourceVertBuffer.SetData(setA);
            targetVertBuffer.SetData(setB);

            // Find the needed dispatch size, so that each distance pair will be run over
            _computeShader.GetKernelThreadGroupSizes(kernel, out uint threadGroupX, out uint threadGroupY, out _);
            int xDispSize = Mathf.CeilToInt((float)numPtsA / threadGroupX);
            int yDispSize = Mathf.CeilToInt((float)numPtsB / threadGroupY);
            // Dispatch the compute shader
            _computeShader.Dispatch(kernel, xDispSize, yDispSize, 1);

            // Get the data from the compute shader
            // Unity will wait here until the compute shader finishes
            // CPU read will wait for any GPU writes. Look into AsyncGPUReadback.
            distBuffer.GetData(distances);

            // Make output matrix
            distanceMatrix = Matrix<float>.Build.DenseOfRowMajor(numPtsA, numPtsB, distances);

            // Release the graphics buffers, disposing them
            sourceVertBuffer.Release();
            targetVertBuffer.Release();
            distBuffer.Release();

            return true;
        }

        [BurstCompile]
        public unsafe struct DistanceJob : IJobParallelFor
        {
            [ReadOnly] public int numPtsA;
            [ReadOnly] public int numPtsB;

            [NativeDisableUnsafePtrRestriction] [ReadOnly]
            public float3* setA;

            [NativeDisableUnsafePtrRestriction] [ReadOnly]
            public float3* setB;

            [NativeDisableUnsafePtrRestriction] [WriteOnly]
            public float* distances;

            public float gamma;
            public int rbfType; // 0: linear, 1: gaussian, 2: multi quadratic

            public void Execute(int index)
            {
                int i = index / numPtsB;
                int j = index % numPtsB;

                if (i >= numPtsA || j >= numPtsB)
                {
                    return;
                }

                float3 posA = setA[i];
                float3 posB = setB[j];
                float dist = math.distance(posA, posB);

                // Apply Radial Basis Function (RBF) if needed
                float result = dist; // Linear by default
                if (rbfType == 1) // Gaussian
                {
                    result = math.exp(-(dist * dist) / (2.0f * gamma * gamma));
                }
                else if (rbfType == 2) // Multi Quadratic
                {
                    result = math.sqrt((dist * dist) + (gamma * gamma));
                }

                // Write to the storage array in column-major order
                int storageIndex = i + j * numPtsA; // Column-major order
                distances[storageIndex] = result;
            }
        }

        public unsafe bool TryComputeDistanceMatrix(Vector3[] setAArray, Vector3[] setBArray, out Matrix<float> distanceMatrix, float gamma = 1.0f, int rbfType = 0)
        {
            distanceMatrix = null;
            if (setAArray == null || setBArray == null)
            {
                return false;
            }

            int numPtsA = setAArray.Length;
            int numPtsB = setBArray.Length;
            if (numPtsA <= 0 || numPtsB <= 0)
            {
                return false;
            }

            // Pin the arrays to prevent GC from moving them
            GCHandle handleA = GCHandle.Alloc(setAArray, GCHandleType.Pinned);
            GCHandle handleB = GCHandle.Alloc(setBArray, GCHandleType.Pinned);

            // Create the distance matrix and get its storage array
            float[] distancesArray = new float[numPtsA * numPtsB];
            distanceMatrix = new DenseMatrix(numPtsA, numPtsB, distancesArray);
            GCHandle handleDistances = GCHandle.Alloc(distancesArray, GCHandleType.Pinned);

            try
            {
                // Get pointers to the pinned data
                float3* setAPtr = (float3*)handleA.AddrOfPinnedObject();
                float3* setBPtr = (float3*)handleB.AddrOfPinnedObject();
                float* distancesPtr = (float*)handleDistances.AddrOfPinnedObject();

                // Set up and schedule the job
                DistanceJob job = new DistanceJob
                {
                    numPtsA = numPtsA,
                    numPtsB = numPtsB,
                    setA = setAPtr,
                    setB = setBPtr,
                    distances = distancesPtr,
                    gamma = gamma,
                    rbfType = rbfType
                };

                JobHandle handle = job.Schedule(numPtsA * numPtsB, 64);
                handle.Complete();
            }
            finally
            {
                // Free the pinned handles
                handleA.Free();
                handleB.Free();
                handleDistances.Free();
            }

            return true;
        }
    }
}
