using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

namespace Genies.Avatars
{
    public struct SourceData
    {
        public Vector3[] uniquePoints;
        public Matrix<float> srcDistanceMatrix;

        public SourceData(in Vector3[] points, in Matrix<float> distmat)
        {
            uniquePoints = points;
            srcDistanceMatrix = distmat;
        }
    }
}