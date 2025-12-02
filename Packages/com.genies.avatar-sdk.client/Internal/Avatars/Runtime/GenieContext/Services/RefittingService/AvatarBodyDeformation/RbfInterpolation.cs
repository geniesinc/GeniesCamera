using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.Utilities;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using UnityEngine.Profiling;

namespace Genies.Avatars
{
    public sealed class RbfInterpolation
    {
        private readonly DistanceComputeDispatcher _computeDispatcher;

        public RbfInterpolation()
        {
            _computeDispatcher = new DistanceComputeDispatcher();
        }

        // runs on main thread (limitation for Compute shaders)
        public void ComputeDistanceMatrix(in Vector3[] setA, in Vector3[] setB, out Matrix<float> distanceMatrix)
        {
            if (_computeDispatcher.TryComputeDistanceMatrix(setA, setB, out distanceMatrix))
            {
                return;
            }

            Debug.LogError($"[{nameof(RbfInterpolation)}] failed to compute distance matrix");
        }

        public async Task<Vector3[]> DeformTargetAsync(Vector3[] targetPoints, Vector3[] sourcePoints, Matrix<float> weightMatrix)
        {
            await OperationQueue.EnqueueAsync();
            ComputeDistanceMatrix(targetPoints, sourcePoints, out Matrix<float> tgtDistMatrix);

            Vector3[] result = await UniTask.RunOnThreadPool(() => DeformTarget(targetPoints, tgtDistMatrix, weightMatrix));

            return result;
        }

        private Vector3[] DeformTarget(in Vector3[] targetPoints, in Matrix<float> tgtDistMatrix, in Matrix<float> weightMatrix)
        {
            Matrix<float> targetMat = MakePointArray(targetPoints);
            Matrix<float> outPoints = GetTransformedPointsFromWeightMatrix(weightMatrix, targetMat, tgtDistMatrix);
            return MatrixPointsToDeltas(outPoints, targetPoints);
        }

        public Task<Matrix<float>> CalcRBFDriverDataAsync(string solveKey, Vector3[] sourcePoints, Vector3[] deformPoints, Matrix<float> sourceDistMatrix)
        {
            return Task.Run(() =>
            {
                Profiler.BeginThreadProfiling("rbf solve threads", solveKey);
                return RunRBFDriverSolve(sourcePoints, deformPoints, sourceDistMatrix);
            });
        }

        public Matrix<float> RunRBFDriverSolve(in Vector3[] sourcePoints, in Vector3[] deformPoints, in Matrix<float> sourceDistMatrix)
        {
            Matrix<float> sourceMat = MakePointArray(sourcePoints);
            Matrix<float> defMat = MakePointArray(deformPoints);
            return GetWeightMatrix(sourcePoints.Length, sourceMat, defMat, sourceDistMatrix);
        }

        // Get weight matrix 'theta' for calculating refit:
        // A * theta = B
        // We want to do linear solve for nx3 matrix theta ~O(n^3)
        private Matrix<float> GetWeightMatrix(int numUniquePts, Matrix<float> sourcePoints, Matrix<float> defPoints, Matrix<float> distMatrix)
        {
            Matrix<float> identity = Matrix<float>.Build.Dense(numUniquePts, 1, 1.0F);
            int dim = 3;
            Matrix<float> r1 = distMatrix.Append(identity).Append(sourcePoints);
            Matrix<float> r2 = identity.Transpose().Append(Matrix<float>.Build.Dense(1, 1, 0F))
                .Append(Matrix<float>.Build.Dense(1, dim, 0F));
            Matrix<float> r3 = sourcePoints.Transpose().Append(Matrix<float>.Build.Dense(dim, 1, 0F))
                .Append(Matrix<float>.Build.Dense(dim, dim, 0F));

            Matrix<float> A = r1.Stack(r2).Stack(r3);

            //if (System.Math.Abs(A.Determinant()) < 0.001f)
            //UnityEngine.Debug.Log($"[{nameof(RbfInterpolation)}] input matrix A is singular (not invertible)");

            Matrix<float> B = defPoints.Stack(Matrix<float>.Build.Dense(1, dim, 0F))
                .Stack(Matrix<float>.Build.Dense(dim, dim, 0F));

            // Get pseudoinverse and calculate theta via linear regression function:
            // Calculation must be done at double precision or it will be distorted by rounding errors.
            Matrix<double> dA = A.Map(x => (double)x);
            double regularization_parameter = 0.00001; // TODO: expose
            Matrix<double> r = Matrix<double>.Build.Dense(A.RowCount, A.ColumnCount, 0.0);
            for (int n = 0; n < A.RowCount; n++)
            {
                r[n, n] = regularization_parameter;
            }

            Matrix<double> tA = dA.Transpose();
            Matrix<double> pseudoInverse = CustomPinv(tA * dA + r) * tA;
            // boost B to double precision for the final mult
            Matrix<double> theta = pseudoInverse * B.Map(x => (double)x);
            Profiler.EndThreadProfiling(); // end profiling here b/c of return
            return theta.Map(x => (float)x);
        }

        // Custom pseudoinverse function, uses double precision and Singular Value Decomposition
        private static Matrix<double> CustomPinv(Matrix<double> matrix)
        {
            var svd = matrix.Svd(true);
            Matrix<double> W = svd.W;
            Vector<double> s = svd.S;

            for (int i = 0; i < s.Count; i++)
            {
                if (s[i] != 0)
                {
                    s[i] = 1 / s[i];
                }
            }

            for (int n = 0; n < W.RowCount; n++)
            {
                W[n, n] = 1.0 / W[n, n];
            }

            // (U * W * VT)T is equivalent with V * WT * UT
            return (svd.U * W * svd.VT).Transpose();
        }

        // construct N x 3 matrix
        private Matrix<float> MakePointArray(in Vector3[] input)
        {
            float[,] arr = new float[input.Length, 3];

            for (int pnt = 0; pnt < input.Length; pnt++)
            {
                arr[pnt, 0] = input[pnt].x;
                arr[pnt, 1] = input[pnt].y;
                arr[pnt, 2] = input[pnt].z;
            }

            return Matrix<float>.Build.DenseOfArray(arr);
        }

        // Perform equivalent linear transformation on un-deformed input points using weight matrix (theta) from training points
        private static Matrix<float> GetTransformedPointsFromWeightMatrix(Matrix<float> weightMatrix, Matrix<float> targetPoints, Matrix<float> targetDist)
        {
        // Transform input points into a distance matrix with points appended, as was done with the source points earlier
            Matrix<float> identity = Matrix<float>.Build.Dense(targetPoints.RowCount, 1, 1F);
            Matrix<float> H = targetDist.Append(identity).Append(targetPoints); // Append=concat mat to right

            return H.Multiply(weightMatrix);
        }

        private static Vector3[] MatrixPointsToDeltas(Matrix<float> ptsMat, in Vector3[] origPoints)
        {
            float[,] arr = ptsMat.ToArray();

            if (arr.GetLength(0) != origPoints.Length)
            {
                Debug.LogError($"[{nameof(RbfInterpolation)}] point size mismatch in refit results");
                return null;
            }

            Vector3[] deltas = new Vector3[origPoints.Length];
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                deltas[i] = new Vector3(arr[i, 0] - origPoints[i].x, arr[i, 1] - origPoints[i].y,
                    arr[i, 2] - origPoints[i].z);
            }

            return deltas;
        }
    }
}
