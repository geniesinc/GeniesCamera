#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// Utilities for drawing debug graphics for dynamics in the Unity scene view
    /// </summary>
    public static class DynamicsGizmos
    {
        private const float _defaultAxisScale = 0.1f;

        public static void DrawAxes(Vector3 position, Quaternion rotation, float scale = _defaultAxisScale)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(position, position + rotation * Vector3.right * scale);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(position, position + rotation * Vector3.up * scale);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(position, position + rotation * Vector3.forward * scale);
        }

        public static void DrawWireCapsule(Vector3 position, Quaternion rotation, float radius, float height, Color color = default)
        {
            if (color != default)
                Handles.color = color;

            var angleMatrix = Matrix4x4.TRS(position, rotation, Handles.matrix.lossyScale);
            using (new Handles.DrawingScope(angleMatrix))
            {
                var pointOffset = height / 2;

                //draw sideways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, radius);
                Handles.DrawLine(new Vector3(0, pointOffset, -radius), new Vector3(0, -pointOffset, -radius));
                Handles.DrawLine(new Vector3(0, pointOffset, radius), new Vector3(0, -pointOffset, radius));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, radius);
                //draw frontways
                Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, radius);
                Handles.DrawLine(new Vector3(-radius, pointOffset, 0), new Vector3(-radius, -pointOffset, 0));
                Handles.DrawLine(new Vector3(radius, pointOffset, 0), new Vector3(radius, -pointOffset, 0));
                Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, radius);
                //draw center
                Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, radius);
                Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, radius);

            }
        }

        public static void DrawWireCone(Vector3 tipPosition, float height, float radius, Quaternion rotation, Color color, int numSegments = 7)
        {
            Gizmos.color = color;
            var angleStep = 360f / numSegments;
            Vector3 previousPoint = Vector3.zero;

            Vector3 basePosition = tipPosition - rotation * Vector3.forward * height;

            for (var i = 0; i <= numSegments; i++)
            {
                var angle = i * angleStep * Mathf.Deg2Rad;
                var point = new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0);
                point = basePosition + rotation * point;

                if (i > 0)
                {
                    Gizmos.DrawLine(tipPosition, point);
                    Gizmos.DrawLine(previousPoint, point);
                }

                previousPoint = point;
            }
        }

        public static Mesh CreateCapsuleMesh(float radius, float height, int segments = 16, int rings = 8)
        {
            Mesh mesh = new();

            int totalVertices = segments * (2 * rings + 1);
            Vector3[] vertices = new Vector3[totalVertices];
            int[] triangles = new int[segments * rings * 12];

            float segmentAngle = Mathf.PI * 2f / segments;
            float ringAngle = Mathf.PI / (2 * rings);

            // Generate vertices
            for (int j = 0; j <= 2 * rings; j++)
            {
                for (int i = 0; i < segments; i++)
                {
                    float angle = segmentAngle * i;
                    float vAngle = ringAngle * j - Mathf.PI / 2f; // vertical angle [-90, 90]
                    float x = Mathf.Cos(angle) * Mathf.Cos(vAngle) * radius;
                    float y = Mathf.Sin(vAngle) * radius;
                    if (j < rings) y -= height / 2f; // bottom hemisphere
                    if (j > rings) y += height / 2f; // top hemisphere
                    float z = Mathf.Sin(angle) * Mathf.Cos(vAngle) * radius;

                    vertices[j * segments + i] = new Vector3(x, y, z);
                }
            }

            // Generate triangles
            for (int j = 0; j < 2 * rings; j++)
            {
                for (int i = 0; i < segments; i++)
                {
                    int i2 = (i + 1) % segments;
                    int offset = (j * segments + i) * 6;

                    triangles[offset + 0] = j * segments + i;
                    triangles[offset + 1] = (j + 1) * segments + i2;
                    triangles[offset + 2] = (j + 1) * segments + i;

                    triangles[offset + 3] = j * segments + i;
                    triangles[offset + 4] = j * segments + i2;
                    triangles[offset + 5] = (j + 1) * segments + i2;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
#endif
