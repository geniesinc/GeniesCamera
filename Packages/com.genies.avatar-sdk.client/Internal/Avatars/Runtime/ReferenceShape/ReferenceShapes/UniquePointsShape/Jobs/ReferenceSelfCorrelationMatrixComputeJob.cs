using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Computes the self correlation matrix for the reference points. Which encodes the distances between each pair
    /// of points, plus the reference points themselves. This self correlation matrix is also squared, meaning that it
    /// adds extra rows at the end which is the transpose of the reference points columns. This job is used by
    /// <see cref="UniquePointsShape"/> to transfer deforms.
    /// </summary>
    [BurstCompile]
    public struct ReferenceSelfCorrelationMatrixComputeJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public NativeMatrix CorrelationMatrix;

        [ReadOnly] private readonly NativeArray<Vector3> _referencePoints;

        public ReferenceSelfCorrelationMatrixComputeJob(NativeArray<Vector3> referencePoints, Allocator allocator)
        {
            CorrelationMatrix = new NativeMatrix(referencePoints.Length + 4, referencePoints.Length + 4, allocator, NativeArrayOptions.UninitializedMemory);

            _referencePoints = referencePoints;
            
            // get fixed indices for the bias term and the x, y, z rows and columns
            int biasTermRowIndex = referencePoints.Length * CorrelationMatrix.Cols;
            int xRowIndex        = biasTermRowIndex + CorrelationMatrix.Cols;
            int yRowIndex        = xRowIndex + CorrelationMatrix.Cols;
            int zRowIndex        = yRowIndex + CorrelationMatrix.Cols;
            
            int biasTermColIndex = referencePoints.Length;
            int xColIndex        = referencePoints.Length + 1;
            int yColIndex        = referencePoints.Length + 2;
            int zColIndex        = referencePoints.Length + 3;
            
            // populate the bias term and x, y, z rows and columns
            for (int i = 0; i < referencePoints.Length; ++i)
            {
                int offset = i * CorrelationMatrix.Cols;
                CorrelationMatrix.Data[offset + biasTermColIndex] = 1.0f;
                CorrelationMatrix.Data[offset + xColIndex       ] = referencePoints[i].x;
                CorrelationMatrix.Data[offset + yColIndex       ] = referencePoints[i].y;
                CorrelationMatrix.Data[offset + zColIndex       ] = referencePoints[i].z;
                
                CorrelationMatrix.Data[biasTermRowIndex + i] = 1.0f;
                CorrelationMatrix.Data[xRowIndex        + i] = referencePoints[i].x;
                CorrelationMatrix.Data[yRowIndex        + i] = referencePoints[i].y;
                CorrelationMatrix.Data[zRowIndex        + i] = referencePoints[i].z;
            }
            
            // populate the lower right corner of the matrix with zeros
            for (int row = referencePoints.Length; row < CorrelationMatrix.Rows; ++row)
            {
                int offset = row * CorrelationMatrix.Cols;
                for (int col = referencePoints.Length; col < CorrelationMatrix.Cols; ++col)
                {
                    CorrelationMatrix.Data[offset + col] = 0.0f;
                }
            }
        }

        public void Execute(int index)
        {
            Vector3 point    = _referencePoints[index];
            int     rowIndex = index * CorrelationMatrix.Cols;
            
            // compute the distances with each other reference point
            for (int i = 0; i < _referencePoints.Length; ++i)
            {
                CorrelationMatrix.Data[rowIndex + i] = Vector3.Distance(_referencePoints[i], point);
            }
        }
    }
}