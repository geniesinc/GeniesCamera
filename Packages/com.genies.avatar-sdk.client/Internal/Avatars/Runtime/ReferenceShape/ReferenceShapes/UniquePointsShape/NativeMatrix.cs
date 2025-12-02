using System;
using System.Runtime.CompilerServices;
using MathNet.Numerics.LinearAlgebra;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Custom matrix implementation using NativeArray for better performance in Unity Jobs. It is specifically designed
    /// for <see cref="UniquePointsShape"/> so it lacks a lot of features and is not a general-purpose matrix.
    /// </summary>
    public struct NativeMatrix : IDisposable
    {
        public int                Rows;
        public int                Cols;
        public NativeArray<float> Data;
        
        public NativeMatrix(int rows, int cols, NativeArray<float> data)
        {
            Rows = rows;
            Cols = cols;
            Data = data;
        }
        
        public NativeMatrix(int rows, int cols, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Rows = rows;
            Cols = cols;
            
            Data = new NativeArray<float>(rows * cols, allocator, options);
        }
        
        public NativeMatrix(Matrix<float> source, Allocator allocator)
        {
            Rows = source.RowCount;
            Cols = source.ColumnCount;
            
            Data = new NativeArray<float>(source.RowCount * source.ColumnCount, allocator, NativeArrayOptions.UninitializedMemory);
            
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Cols; column++)
                {
                    Set(row, column, source[row, column]);
                }
            }
        }
        
        public NativeMatrix(Matrix<double> source, Allocator allocator)
        {
            Rows = source.RowCount;
            Cols = source.ColumnCount;
            
            Data = new NativeArray<float>(source.RowCount * source.ColumnCount, allocator, NativeArrayOptions.UninitializedMemory);
            
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Cols; column++)
                {
                    Set(row, column, (float)source[row, column]);
                }
            }
        }
        
        public NativeMatrix(Serializable serializable, Allocator allocator)
        {
            Rows = serializable.rows;
            Cols = serializable.cols;
            Data = new NativeArray<float>(serializable.data, allocator);
        }

        public Matrix<float> ToMatrix()
        {
            Matrix<float> matrix = Matrix<float>.Build.Dense(Rows, Cols);
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Cols; column++)
                {
                    matrix[row, column] = Get(row, column);
                }
            }

            return matrix;
        }
        
        public Matrix<double> ToDoubleMatrix()
        {
            Matrix<double> matrix = Matrix<double>.Build.Dense(Rows, Cols);
            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Cols; column++)
                {
                    matrix[row, column] = Get(row, column);
                }
            }

            return matrix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Get(int row, int column)
            => Data[row * Cols + column];
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Set(int row, int column, float value)
            => Data[row * Cols + column] = value;

        public NativeMatrix Multiply(NativeMatrix other, Allocator resultAllocator)
        {
            if (Cols != other.Rows)
            {
                throw new ArgumentException($"Matrix dimensions are not compatible for multiplication: {Rows}x{Cols} and {other.Rows}x{other.Cols}");
            }

            var result = new NativeMatrix(Rows, other.Cols, resultAllocator, NativeArrayOptions.UninitializedMemory);
            var job = new MultiplyJob(this, other, result);
            int iterations = Rows * other.Cols;
            job.Schedule(iterations, iterations / JobsUtility.MaxJobThreadCount).Complete();
            
            return result;
        }

        public void Multiply(NativeMatrix other, NativeMatrix result)
        {
            if (Cols != other.Rows)
            {
                throw new ArgumentException($"Matrix dimensions are not compatible for multiplication: {Rows}x{Cols} and {other.Rows}x{other.Cols}");
            }

            if (result.Rows != Rows || result.Cols != other.Cols)
            {
                throw new ArgumentException($"Result matrix dimensions are not compatible: {result.Rows}x{result.Cols} and {Rows}x{other.Cols}");
            }

            var job = new MultiplyJob(this, other, result);
            int iterations = Rows * other.Cols;
            job.Schedule(iterations, iterations / JobsUtility.MaxJobThreadCount).Complete();
        }
        
        public NativeArray<Vector3> MultiplyIntoPoints(NativeMatrix other, Allocator resultAllocator)
        {
            if (Cols != other.Rows)
            {
                throw new ArgumentException($"Matrix dimensions are not compatible for multiplication: {Rows}x{Cols} and {other.Rows}x{other.Cols}");
            }

            if (other.Cols != 3)
            {
                throw new ArgumentException($"The other matrix must have 3 columns to represent points: {other.Rows}x{other.Cols}");
            }

            var result = new NativeArray<Vector3>(Rows, resultAllocator, NativeArrayOptions.UninitializedMemory);
            var job = new MultiplyIntoPointsJob(this, other, result);
            job.Schedule(Rows, Rows / JobsUtility.MaxJobThreadCount).Complete();
            
            return result;
        }

        public void MultiplyIntoPoints(NativeMatrix other, NativeArray<Vector3> result)
        {
            if (Cols != other.Rows)
            {
                throw new ArgumentException($"Matrix dimensions are not compatible for multiplication: {Rows}x{Cols} and {other.Rows}x{other.Cols}");
            }

            if (other.Cols != 3)
            {
                throw new ArgumentException($"The other matrix must have 3 columns to represent points: {other.Rows}x{other.Cols}");
            }

            if (result.Length != Rows)
            {
                throw new ArgumentException($"Result points length is not compatible: {result.Length} points and {Rows}x{other.Cols} matrix");
            }

            var job = new MultiplyIntoPointsJob(this, other, result);
            job.Schedule(Rows, Rows / JobsUtility.MaxJobThreadCount).Complete();
        }
        
        public NativeArray<Vector3> MultiplyIntoDeltas(NativeMatrix other, NativeArray<Vector3> points, Allocator resultAllocator)
        {
            if (Cols != other.Rows)
            {
                throw new ArgumentException($"Matrix dimensions are not compatible for multiplication: {Rows}x{Cols} and {other.Rows}x{other.Cols}");
            }

            if (other.Cols != 3)
            {
                throw new ArgumentException($"The other matrix must have 3 columns to represent points: {other.Rows}x{other.Cols}");
            }

            if (points.Length != Rows)
            {
                throw new ArgumentException($"Points length is not compatible: {points.Length} points and {Rows}x{other.Cols} matrix");
            }

            var deltas = new NativeArray<Vector3>(Rows, resultAllocator, NativeArrayOptions.UninitializedMemory);
            var job = new MultiplyIntoDeltasJob(this, other, points, deltas);
            job.Schedule(Rows, Rows / JobsUtility.MaxJobThreadCount).Complete();
            
            return deltas;
        }

        public void MultiplyIntoDeltas(NativeMatrix other, NativeArray<Vector3> points, NativeArray<Vector3> deltas)
        {
            if (Cols != other.Rows)
            {
                throw new ArgumentException($"Matrix dimensions are not compatible for multiplication: {Rows}x{Cols} and {other.Rows}x{other.Cols}");
            }

            if (other.Cols != 3)
            {
                throw new ArgumentException($"The other matrix must have 3 columns to represent points: {other.Rows}x{other.Cols}");
            }

            if (points.Length != Rows)
            {
                throw new ArgumentException($"Points length is not compatible: {points.Length} points and {Rows}x{other.Cols} matrix");
            }

            if (points.Length != deltas.Length)
            {
                throw new ArgumentException($"Deltas length is not the same as points length: {points.Length} points and {deltas.Length} deltas");
            }

            var job = new MultiplyIntoDeltasJob(this, other, points, deltas);
            job.Schedule(Rows, Rows / JobsUtility.MaxJobThreadCount).Complete();
        }

        public NativeMatrix PseudoInverse(Allocator allocator)
        {
            /**
             * Not the most efficient way since we are doing a lot of managed allocations here. We should use a native
             * call and use Eigen's pseudoinverse method (completeOrthogonalDecomposition().pseudoInverse()).
             */
            return new NativeMatrix(ToDoubleMatrix().PseudoInverse(), allocator);
        }

        public void Dispose()
        {
            Data.Dispose();
            Data = default;
        }
        
        [Serializable]
        public struct Serializable
        {
            public int     rows;
            public int     cols;
            public float[] data;
            
            public Serializable(NativeMatrix matrix)
            {
                rows = matrix.Rows;
                cols = matrix.Cols;
                data = matrix.Data.ToArray();
            }
        }
        
        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            [ReadOnly]  private readonly NativeMatrix _a;
            [ReadOnly]  private readonly NativeMatrix _b;
            [WriteOnly] private          NativeMatrix _result;
            
            public MultiplyJob(NativeMatrix a, NativeMatrix b, NativeMatrix result)
            {
                _a      = a;
                _b      = b;
                _result = result;
            }
            
            public void Execute(int index)
            {
                int row = index / _result.Cols;
                int column = index % _result.Cols;
                
                int aIndexOffset = row * _a.Cols;
                float value = 0;
                
                for (int i = 0; i < _a.Cols; i++)
                {
                    value += _a.Data[aIndexOffset + i] * _b.Data[i * _b.Cols + column];
                }

                _result.Data[row * _result.Cols + column] = value;
            }
        }
        
        [BurstCompile]
        private struct MultiplyIntoPointsJob : IJobParallelFor
        {
            [ReadOnly]  private readonly NativeMatrix _a;
            [ReadOnly]  private readonly NativeMatrix _b;
            [WriteOnly] private NativeArray<Vector3>  _result;
            
            public MultiplyIntoPointsJob(NativeMatrix a, NativeMatrix b, NativeArray<Vector3> result)
            {
                _a      = a;
                _b      = b;
                _result = result;
            }
            
            public void Execute(int index)
            {
                int aIndexOffset = index * _a.Cols;
                Vector3 point = Vector3.zero;

                for (int i = 0; i < _a.Cols; i++)
                {
                    int   bRowIndex = i * 3;
                    float aValue    = _a.Data[aIndexOffset + i];
                    
                    point.x += aValue * _b.Data[bRowIndex + 0];
                    point.y += aValue * _b.Data[bRowIndex + 1];
                    point.z += aValue * _b.Data[bRowIndex + 2];
                }
                
                _result[index] = point;
            }
        }
        
        [BurstCompile]
        private struct MultiplyIntoDeltasJob : IJobParallelFor
        {
            [ReadOnly]  private readonly NativeMatrix         _a;
            [ReadOnly]  private readonly NativeMatrix         _b;
            [ReadOnly]  private readonly NativeArray<Vector3> _points;
            [WriteOnly] private          NativeArray<Vector3> _deltas;
            
            public MultiplyIntoDeltasJob(NativeMatrix a, NativeMatrix b, NativeArray<Vector3> points, NativeArray<Vector3> deltas)
            {
                _a      = a;
                _b      = b;
                _points = points;
                _deltas = deltas;
            }
            
            public void Execute(int index)
            {
                int aIndexOffset = index * _a.Cols;
                Vector3 point = Vector3.zero;

                for (int i = 0; i < _a.Cols; i++)
                {
                    int   bRowIndex = i * 3;
                    float aValue    = _a.Data[aIndexOffset + i];
                    
                    point.x += aValue * _b.Data[bRowIndex + 0];
                    point.y += aValue * _b.Data[bRowIndex + 1];
                    point.z += aValue * _b.Data[bRowIndex + 2];
                }
                
                _deltas[index] = point - _points[index];
            }
        }
    }
}