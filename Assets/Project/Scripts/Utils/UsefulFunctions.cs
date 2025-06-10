

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARKit;

public static class UsefulFunctions
{
    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input[0].ToString().ToUpper() + input.Substring(1)
        };

    public static Transform FindChildRecursively(this Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform result = FindChildRecursively(child, childName);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static void SetLayersRecursively(this Transform parent, int newLayer)
    {
        // Change the layer of the current object
        parent.gameObject.layer = newLayer;

        // Recursively change the layer of all child objects
        for (int i = 0; i < parent.childCount; i++)
        {
            SetLayersRecursively(parent.GetChild(i), newLayer);
        }
    }

    public static float GetSmallestEulerValue(this float angle)
    {
        if (angle > 180)
        {
            return angle - 360;
        }
        else if (angle < -180)
        {
            return angle + 360;
        }
        return angle;
    }

    public static float GetPositiveEulerValue(this float angle)
    {
        if (angle < 0)
        {
            return angle + 360;
        }
        return angle;
    }

    public static float GetAspectRatio(this Vector2 rect)
    {
        if (rect.y == 0)
        {
            return 0;
        }
        return rect.x / rect.y;
    }

    public static Quaternion GetMirror(this Quaternion q)
    {
        q.x *= -1f;
        q.w *= -1f;
        return q;
    }

    public static Quaternion ExtractYaw(this Quaternion q)
    {
        q.x = 0;
        q.z = 0;
        float mag = Mathf.Sqrt(q.w * q.w + q.y * q.y);
        q.w /= mag;
        q.y /= mag;
        return q;
    }

    public static Quaternion ExtractPitch(this Quaternion q)
    {
        q.y = 0;
        q.z = 0;
        float mag = Mathf.Sqrt(q.w * q.w + q.x * q.x);
        q.w /= mag;
        q.x /= mag;
        return q;
    }

    public static Quaternion ExtractRoll(this Quaternion q)
    {
        q.x = 0;
        q.y = 0;
        float mag = Mathf.Sqrt(q.w * q.w + q.z * q.z);
        q.w /= mag;
        q.z /= mag;
        return q;
    }

    public static bool CanPlaceOnSurfaceWithNormal(this Vector3 surfaceNormal)
    {
        return Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) >= 0.5f;
    }

    public static bool IsGazeBlendshape(this ARKitBlendShapeLocation location)
    {
        return location == ARKitBlendShapeLocation.EyeLookUpRight ||
               location == ARKitBlendShapeLocation.EyeLookUpLeft ||
               location == ARKitBlendShapeLocation.EyeLookDownLeft ||
               location == ARKitBlendShapeLocation.EyeLookDownRight ||
               location == ARKitBlendShapeLocation.EyeLookInRight ||
               location == ARKitBlendShapeLocation.EyeLookInLeft ||
               location == ARKitBlendShapeLocation.EyeLookOutLeft ||
               location == ARKitBlendShapeLocation.EyeLookOutRight;
    }
    
    public static Mesh CombineMeshes(SkinnedMeshRenderer[] renderers)
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        foreach (var skinnedMeshRenderer in renderers)
        {
            CombineInstance combineInstance = new CombineInstance
            {
                mesh = skinnedMeshRenderer.sharedMesh
            };
            combineInstances.Add(combineInstance);
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances.ToArray(),
                                    mergeSubMeshes: true,
                                    useMatrices: false);
        return combinedMesh;
    }

    public static Vector3 GetHighestVertex(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        if(vertices.Length == 0)
        {
            return Vector3.zero;
        }

        Vector3 highestVert = vertices[0];
        for(int i=0; i < vertices.Length; i ++)
        {
            if(vertices[i].y > highestVert.y)
            {
                highestVert = vertices[i];
            }
        }
        return highestVert;
    }

    /// <summary>
    /// Raycast a mesh and return the closest point of intersection.
    /// </summary>
    /// <param name="rayOriginWorld">Ray's origin point.</param>
    /// <param name="rayDir">The magnitude of this is the maximum distance of the raycast.</param>
    /// <param name="mesh">The mesh we are casting against.</param>
    /// <param name="meshTransformIfNotAtOrigin">The Transform of the GameObject the mesh belongs to, so we can calculate the local space, which is the space the vertices live in...</param>
    /// <returns></returns>
    public static Vector3? RaycastGeoInLocalSpace(Vector3 rayOriginLocal,
                                                Vector3 rayDir,
                                                Vector3[] vertices,
                                                int[] triangles)
    {
        float maxRayLength = 5f;
        Vector3? mostProtrudingIntersectionPoint = null;

        // This will break down if it's like, a unsymmetrical hat or something.
        // But for SOME REASON, rn, the Genie is naked? So it's actually fine?
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            if (DoesRayIntersectTriangleEitherSide(rayOriginLocal,
                                                rayDir.normalized,
                                                v0, v1, v2,
                                                out Vector3 intersectionPoint))
            {
                float distToIntersectionPoint = Vector3.Distance(rayOriginLocal,
                                                                 intersectionPoint);
                if (distToIntersectionPoint <= maxRayLength)
                {
                    maxRayLength = distToIntersectionPoint;
                    mostProtrudingIntersectionPoint = intersectionPoint;
                }
            }
        }

        Debug.DrawRay(rayOriginLocal,
                      rayDir.normalized * maxRayLength,
                      mostProtrudingIntersectionPoint.HasValue ? Color.green : Color.red,
                      5000f);
        Debug.Log("RayOriginLocal: " + rayOriginLocal + ", Hit: " + mostProtrudingIntersectionPoint.HasValue);

        return mostProtrudingIntersectionPoint;
    }

    public static bool DoesRayIntersectTriangleEitherSide(Vector3 rayOrigin, Vector3 rayDir,
                                            Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(rayDir, edge2);
        float a = Vector3.Dot(edge1, h);

        // Allow hits from both sides by taking the absolute value of 'a'
        if (Mathf.Abs(a) < 1e-6f)
            return false; // Ray parallel to triangle

        float f = 1.0f / a;
        Vector3 s = rayOrigin - v0;
        float u = f * Vector3.Dot(s, h);

        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(rayDir, q);

        if (v < 0.0f || u + v > 1.0f)
            return false;

        float t = f * Vector3.Dot(edge2, q);

        if (t > 1e-6f)
        {
            hitPoint = rayOrigin + rayDir * t;
            return true;
        }

        return false;
    }

    public static bool DoesRayIntersectTriangleFront(Vector3 rayOrigin, Vector3 rayDir,
                                                Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hitPoint)
    {
        hitPoint = Vector3.zero;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 h = Vector3.Cross(rayDir, edge2);
        float a = Vector3.Dot(edge1, h);

        if (Mathf.Abs(a) < 1e-6f)
            return false; // Ray parallel to triangle

        float f = 1.0f / a;
        Vector3 s = rayOrigin - v0;
        float u = f * Vector3.Dot(s, h);

        if (u < 0.0f || u > 1.0f)
            return false;

        Vector3 q = Vector3.Cross(s, edge1);
        float v = f * Vector3.Dot(rayDir, q);

        if (v < 0.0f || u + v > 1.0f)
            return false;

        float t = f * Vector3.Dot(edge2, q);
        if (t > 1e-6f)
        {
            hitPoint = rayOrigin + rayDir * t;
            return true;
        }

        return false;
    }

}

public static class RectTransformExtensions
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
}



