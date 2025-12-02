using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Convenient way to encapsulate the vertices and indices of a mesh and provide utility methods to work with the triangles.
    /// This struct doesn't own the vertices and indices arrays, so it's the user's responsibility to keep them alive.
    /// </summary>
    public readonly struct MeshTriangles
    {
        public readonly int Count;
        
        private readonly NativeArray<Vector3> _vertices;
        private readonly NativeArray<int>     _indices;
        
        public MeshTriangles(NativeArray<Vector3> vertices, NativeArray<int> indices)
        {
            if (indices.Length % 3 != 0)
            {
                throw new Exception("The indices array must have a length that is a multiple of 3");
            }

            Count = indices.Length / 3;
            
            _vertices = vertices;
            _indices  = indices;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void GetVertices(int triangleIndex, out Vector3 a, out Vector3 b, out Vector3 c)
        {
            int baseIndex = triangleIndex * 3;
            a = _vertices[_indices[baseIndex]];
            b = _vertices[_indices[baseIndex + 1]];
            c = _vertices[_indices[baseIndex + 2]];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 GetVertexA(int triangleIndex)
        {
            int baseIndex = triangleIndex * 3;
            return _vertices[_indices[baseIndex]];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 GetVertexB(int triangleIndex)
        {
            int baseIndex = triangleIndex * 3;
            return _vertices[_indices[baseIndex + 1]];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 GetVertexC(int triangleIndex)
        {
            int baseIndex = triangleIndex * 3;
            return _vertices[_indices[baseIndex + 2]];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4x4 GetLocalToWorldMatrix(int triangleIndex)
        {
            GetVertices(triangleIndex, out Vector3 a, out Vector3 b, out Vector3 c);
            return GetLocalToWorldMatrix(a, b, c);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 GetClosestPoint(int triangleIndex, in Vector3 point)
        {
            GetVertices(triangleIndex, out Vector3 a, out Vector3 b, out Vector3 c);
            return GetClosestPoint(point, a, b, c);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix4x4 GetLocalToWorldMatrix(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            // this scale is used for the W vector calculation to avoid getting a zero matrix on tiny triangles
            const float errorFixScale = 1000.0f;
            
            Vector3 u = c - a;
            Vector3 v = b - a;
            
            // the cross vector's length is the area of the triangle. So we have to make its length the sqrt so it
            // properly represents the scale in the local Z axis
            Vector3 w = Vector3.Cross(u * errorFixScale, v * errorFixScale);
            w /= errorFixScale * Mathf.Sqrt(w.magnitude); // fastest way to make w's length its sqrt. Equivalent to "w = w.normalized * Mathf.Sqrt(w.magnitude)"
            
            return new Matrix4x4
            {
                m00 = u.x,  m01 = v.x,  m02 = w.x,  m03 = a.x,
                m10 = u.y,  m11 = v.y,  m12 = w.y,  m13 = a.y,
                m20 = u.z,  m21 = v.z,  m22 = w.z,  m23 = a.z,
                m30 = 0.0f, m31 = 0.0f, m32 = 0.0f, m33 = 1.0f
            };
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetClosestPoint(in Vector3 point, in Vector3 a, in Vector3 b, in Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = point - a;

            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f)
            {
                return a;
            }

            Vector3 bp = point - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3)
            {
                return b;
            }

            Vector3 cp = point - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6)
            {
                return c;
            }

            float vc = d1 * d4 - d3 * d2;
            float v;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                v = d1 / (d1 - d3);
                return a + v * ab;
            }

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                v = d2 / (d2 - d6);
                return a + v * ac;
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + v * (c - b);
            }

            float denom = 1.0f / (va + vb + vc);
            v = vb * denom;
            float w = vc * denom;
            return a + v * ab + w * ac;
        }
    }
}