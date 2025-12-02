using System;
using System.Threading;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
    
namespace Genies.Components.CreatorTools.TexturePlacement
{
    public class CylinderMeshGenerator : MonoBehaviour
    {
        public float Radius = 1.0f;
        public float Length = 2.0f;
        public int Subdivisions = 16;   // radial subdivisions
        public int LengthwiseSubdivisions = 16;   // virtual lengthwise subdivisions (just for ray position, not for vertex)
        public float WrapDegrees = 30;  // max value is 2*Mathf.PI or 360 degrees

        [HideInInspector]
        public Vector3 CylinderCenter
        {
            // provide cylinder center, or centroid (different from game object center)
            get { return transform.position - (transform.forward * Radius * transform.localScale.z); }
            private set { CylinderCenter = value; }
        }

        private int _avatarLayerMask;

        private MeshFilter _meshFilter;
        public Mesh Mesh => _meshFilter.mesh;

        private int _secondsDelay = 1;
        private CancellationTokenSource _cancellationTokenSource;

        // generate a cylinder, with length along the y axis and wrap degrees centered on the z axis
        // so that if you are looking at it straight on, you will see the outer wall curve away, with
        // normals pointing inward
        public Mesh GenerateCylinderMesh(float radius, float length, float degrees, int subdivisions)
        {
            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            // move 'radius' dist along local z axis
            Vector3 offset = new Vector3(0f, 0f, radius);

            float angleStep = Mathf.Deg2Rad * degrees / subdivisions;
            float halflength = 0.5f * length;
            float ninetydeg = Mathf.PI * 0.5f;

            int s = subdivisions / 2;
            for (int i = 0; i < subdivisions; ++i)
            {
                int n = i - s;  // make center angle 0 degrees
                float angle = n * angleStep;
                float nextAngle = (n + 1) * angleStep;
                // add 90 degrees b/c centered on z
                angle += ninetydeg;
                nextAngle += ninetydeg;

                // Vertices
                //  2 - 3
                //  |   |
                //  0 - 1
                Vector3 vertex0 = new Vector3(radius * Mathf.Cos(angle), -halflength, radius * Mathf.Sin(angle));
                Vector3 vertex1 = new Vector3(radius * Mathf.Cos(nextAngle), -halflength, radius * Mathf.Sin(nextAngle));
                Vector3 vertex2 = new Vector3(radius * Mathf.Cos(angle), halflength, radius * Mathf.Sin(angle));
                Vector3 vertex3 = new Vector3(radius * Mathf.Cos(nextAngle), halflength, radius * Mathf.Sin(nextAngle));

                vertices.Add(vertex0 - offset);
                vertices.Add(vertex1 - offset);
                vertices.Add(vertex2 - offset);
                vertices.Add(vertex3 - offset);

                // UVs
                uvs.Add(new Vector2((float)i / subdivisions, 0));
                uvs.Add(new Vector2((float)(i + 1) / subdivisions, 0));
                uvs.Add(new Vector2((float)i / subdivisions, 1));
                uvs.Add(new Vector2((float)(i + 1) / subdivisions, 1));

                // Triangles
                // use ccw winding order so that the normals point inwards
                int baseIndex = i * 4;
                // 0 1 2
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                // 2 1 3
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 3);
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();

            return mesh;
        }

        // calculate smooth normal of adjacent vertices
        // with simple average (equal weighting)
        private Vector3 averageNormals(Vector3 n1, Vector3 n2)
        {
            Vector3 smoothN = (n1 + n2) * 0.5f;
            return smoothN.normalized;
        }

        public QueryParameters[] GetSingleTile2CornerRaysQ()
        {
            var queries = new QueryParameters[2];

            queries[0] = new QueryParameters
            {
                layerMask = _avatarLayerMask,
                hitTriggers = QueryTriggerInteraction.UseGlobal,
            };

            queries[1] = new QueryParameters
            {
                layerMask = _avatarLayerMask,
                hitTriggers = QueryTriggerInteraction.UseGlobal,
            };

            return queries;
        }

        public UnityEngine.Ray[] GetRaysFromCurrentMeshRay()
        {
            if (_meshFilter.mesh == null)
            {
                return null;
            }

            var rays = new UnityEngine.Ray[(Subdivisions + 1) * (LengthwiseSubdivisions + 1)];
            Mesh mesh = _meshFilter.mesh;
            int stride = 4;   // 4 vertices per subdiv

            // collect bottom row of vertices and normals
            for (int i = 0; i < Subdivisions; i++)
            {
                int n = i * stride;

                // vertex 0 of current subdiv
                Vector3 org0 = transform.TransformPoint(mesh.vertices[n]);
                Vector3 norm0;
                if (i == 0)
                {
                    // left edge doesn’t need smoothing
                    norm0 = transform.TransformDirection(mesh.normals[n]);
                }
                else
                {
                    // smooth normals between v1 of prev subdiv and v0
                    norm0 = transform.TransformDirection(
                        averageNormals(mesh.normals[((i - 1) * stride) + 1], mesh.normals[n])
                    );
                }

                rays[i] = new UnityEngine.Ray(org0, norm0);

                if (i == (Subdivisions - 1))
                {
                    // right edge (last subdiv) doesn’t need smoothing
                    Vector3 org1 = transform.TransformPoint(mesh.vertices[n + 1]);
                    Vector3 norm1 = transform.TransformDirection(mesh.normals[n + 1]);
                    rays[Subdivisions] = new UnityEngine.Ray(org1, norm1);
                }
            }

            int vstride = Subdivisions + 1;  // columns in each row
            Vector3 offset = transform.up;
            float rise = Length * transform.localScale.y / LengthwiseSubdivisions;
            offset *= rise;

            // replicate rays upwards for vertical sampling
            for (int j = 1; j <= LengthwiseSubdivisions; j++)
            {
                int n = j * vstride;
                offset.y = rise * j;

                for (int i = 0; i <= Subdivisions; i++)
                {
                    UnityEngine.Ray baseRay = rays[i];
                    rays[n + i] = new UnityEngine.Ray(baseRay.origin + offset, baseRay.direction);
                }
            }

            return rays;
        }
        
        // Tile version: rays will be at corners of subdivisions
        // public RaycastCommand[] GetRaysFromCurrentMesh()
        // {
        //     if (_meshFilter.mesh == null)
        //         return null;
        //     var rays = new RaycastCommand[(Subdivisions + 1) * (LengthwiseSubdivisions + 1)];
        //     Mesh mesh = _meshFilter.mesh;
        //     int stride = 4;   // 4 vertices per subdiv
        //                       // collect bottom row, 0, of vertex and smoothed normals, then replicate at different
        //                       // height levels for virtual vertical sampling (for a cylinder, all normals in a column are identical)
        //     for (int i = 0; i < Subdivisions; i++)
        //     {
        //         int n = i * stride;
        //         // for every subdiv, collect vertex 0  (or n+0)
        //         //  on the last subdiv, also collect vertex 1 (n+1)
        //         // interior normals will need smoothing (average with colocated vertex)
        //         Vector3 org0 = transform.TransformPoint(mesh.vertices[n]);
        //         Vector3 norm0;
        //         if (i == 0)
        //         {   // left edge doesn't need smoothing
        //             norm0 = transform.TransformDirection(mesh.normals[n]);
        //         }
        //         else
        //         {   // needs smoothing
        //             norm0 = transform.TransformDirection(averageNormals(mesh.normals[((i - 1) * stride) + 1], mesh.normals[n]));  // v1 of prev subdiv && v0
        //         }
        //         rays[i] = new RaycastCommand(org0, norm0, Mathf.Infinity, _avatarLayerMask);
        //
        //
        //         if (i == (Subdivisions - 1))
        //         {   // right edge doesn't need smoothing
        //             Vector3 org1 = transform.TransformPoint(mesh.vertices[n + 1]);
        //             Vector3 norm1 = transform.TransformDirection(mesh.normals[n + 1]);
        //             // last ray = v1 of last subdiv
        //             rays[Subdivisions] = new RaycastCommand(org1, norm1, Mathf.Infinity, _avatarLayerMask);
        //         }
        //     }
        //     int vstride = Subdivisions + 1;  // # columns in each row
        //     Vector3 offset = transform.up;
        //     float rise = Length * transform.localScale.y / LengthwiseSubdivisions;
        //     offset *= rise;
        //     for (int j = 1; j <= LengthwiseSubdivisions; j++)   // rows 1 -> # lengthwise subdivs [inclusive]
        //     {
        //         int n = j * vstride;
        //         offset.y = rise * j;
        //         for (int i = 0; i <= Subdivisions; i++)
        //         {
        //             RaycastCommand rc = rays[i];
        //             rays[n + i] = new RaycastCommand(rc.from + offset, rc.direction, Mathf.Infinity, _avatarLayerMask);
        //         }
        //     }
        //     return rays;
        // }

        // Point sample version that uses normals
        public RaycastCommand[] GetRayCommandsFromCurrentMesh(int radial, int lengthwise)
        {
            if (_meshFilter.mesh == null)
            {
                return null;
            }

            if (radial % Subdivisions != 0)
            {
                Debug.LogWarning($"radial value {radial} should be a multiple of Subdivisions: {Subdivisions}");
                return null;
            }

            // how many new virtual points to spawn per subdivision
            int multiplier = radial / Subdivisions;
            var rays = new RaycastCommand[(radial + 1) * (lengthwise + 1)];
            var queryParameters = new QueryParameters
            {
                layerMask = _avatarLayerMask,
                hitTriggers = QueryTriggerInteraction.UseGlobal,
            };

            Mesh mesh = _meshFilter.mesh;
            int stride = 4;   // 4 vertices per subdiv
                              // collect bottom row of vertices and smoothed normals, then replicate at different
                              // height levels for virtual vertical sampling (for a cylinder, all normals in a column are identical)
            for (int i = 0; i < Subdivisions; i++)
            {
                int n = i * stride;
                // for every subdiv, collect vertex 0  (or n+0)
                //  and vertex 1 (n+1), then create 'multiplier' # of radial virtual in-between points by
                // lerping between vertex 0 and vertex 1.
                // also lerp between the smoothed (except on outer edges)
                Vector3 org0 = transform.TransformPoint(mesh.vertices[n]);
                Vector3 norm0;
                if (i == 0)
                {   // leftmost edge doesn't need smoothing
                    norm0 = transform.TransformDirection(mesh.normals[n]);
                }
                else
                {   // needs smoothing
                    norm0 = transform.TransformDirection(averageNormals(mesh.normals[((i - 1) * stride) + 1], mesh.normals[n]));  // v1 of prev subdiv && v0
                }

                Vector3 org1 = transform.TransformPoint(mesh.vertices[n + 1]);
                Vector3 norm1;
                if (i == (Subdivisions - 1))
                {   // rightmost edge doesn't need smoothing
                    norm1 = transform.TransformDirection(mesh.normals[n + 1]);
                    // last ray = v1 of last subddiv
                    rays[radial] = new RaycastCommand(org1, norm1, queryParameters, Mathf.Infinity);
                }
                else
                {  // needs smoothing
                    norm1 = averageNormals(mesh.normals[n + 1], mesh.normals[(i + 1) * stride]);  // v1 && v0 of next subdiv
                }

                int m = i * multiplier;   // account for in-between points
                for (int k = 0; k < multiplier; k++)
                {
                    float t = k * 1.0f / multiplier;
                    //_rays[m + k] = new RaycastCommand(Vector3.Lerp(org0, org1, t), Vector3.Slerp(norm0, norm1, t), Mathf.Infinity, _avatarLayerMask);
                    rays[m + k] = new RaycastCommand(Vector3.Lerp(org0, org1, t), norm0, queryParameters, Mathf.Infinity);
                }

            }

            int vstride = radial + 1;  // # columns in each row
            Vector3 offset = transform.up;
            float rise = Length * transform.localScale.y / (float)lengthwise;
            //offset *= rise;
            for (int j = 1; j <= lengthwise; j++)   // rows 1 -> # lengthwise subdivs [inclusive]
            {
                int n = j * vstride;
                offset.y = rise * j;
                //offset *= (rise * (float)j);
                for (int i = 0; i <= radial; i++)  // add row at given rise
                {
                    RaycastCommand rc = rays[i];
                    rays[n + i] = new RaycastCommand(rc.from + offset, rc.direction, rc.queryParameters, rc.distance);
                }
            }

            return rays;
        }

        [BurstCompile]
        public struct CalculateImplicitRaysJob : IJobParallelFor
        {
            public NativeArray<Ray> Rays;
            [ReadOnly]
            public Matrix4x4 Obj2World;
            [ReadOnly]
            public int Cols;
            [ReadOnly]
            public int Rows;

            private Vector3 _offset;
            private Vector3 _point;
            private float _radius, _angleStep, _lengthStep, _ninetydeg;
            private int _hr, _hc;

            public void Init(float radius, float wrapDegrees, float length)
            {
                _offset = new Vector3(0f, 0f, radius);
                _point = new Vector3();

                _angleStep = Mathf.Deg2Rad * wrapDegrees / (float)Cols;
                _lengthStep = length / (float)Rows;
                _ninetydeg = Mathf.PI * 0.5f;
                _radius = radius;

                _hr = Rows / 2;
                _hc = Cols / 2;
            }

            public void Execute(int index)
            {
                int i = (index % Cols) - 1;
                int j = index / Cols;

                // column-wise
                int c = i - _hc;
                float angle = c * _angleStep;
                angle += _ninetydeg;

                // row-wise
                int r = j - _hr;
                float ypos = r * _lengthStep;
                _point.x = _radius * Mathf.Cos(angle); _point.y = ypos; _point.z = _radius * Mathf.Sin(angle);
                Vector3 org = _point - _offset;

                Vector3 toCenter = Vector3.zero - _point;
                toCenter.y = 0;
                Vector3 dir = toCenter.normalized;

                Rays[index] = new Ray(Obj2World.MultiplyPoint3x4(org), Obj2World.MultiplyVector(dir));
            }
        }

        private bool _hasLaunched = false;
        private bool _isCalcRaysJobFinished = false;
        private JobHandle _calcRaysJobHandle;
        /// <summary>
        /// jobified calculation of implicit points and normals that will serve as org/dir for rays
        /// spread uniformly over cylinder grid
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async UniTask<NativeArray<Ray>> GetRaysFromImplicitCylinderAsync(int width, int height)
        {
            int cols = width;
            int rows = height;
            var rays = new NativeArray<Ray>(rows * cols, Allocator.Persistent);
            var calcRaysJob = new CalculateImplicitRaysJob()
            {
                Rays = rays,
                Obj2World = transform.localToWorldMatrix,
                Cols = cols,
                Rows = rows
            };
            calcRaysJob.Init(Radius, WrapDegrees, Length);
            int size = rows * cols;
            int commandsPerJob = Mathf.Max(size / JobsUtility.JobWorkerCount, 1);
            _calcRaysJobHandle = calcRaysJob.Schedule(size, commandsPerJob);
            _hasLaunched = true;
            await UniTask.WaitUntil(() => _isCalcRaysJobFinished == true);
            _isCalcRaysJobFinished = false;
            return rays;
        }

        // point sample that calculates implicit normals - returns array of origin/direction tuples
        public Ray[] GetRaysFromCurrentMesh(int width, int height)
        {
            int cols = width;
            int rows = height;
            Vector3 offset = new Vector3(0f, 0f, Radius);
            Vector3 point = new Vector3();

            float angleStep = Mathf.Deg2Rad * WrapDegrees / (float)cols;
            float lengthStep = Length / (float)rows;
            float ninetydeg = Mathf.PI * 0.5f;

            var rays = new Ray[rows * cols];

            int hr = rows / 2;
            int hc = cols / 2;

            for (int i = 0; i < cols; i++)
            {
                int c = i - hc;
                float angle = c * angleStep;
                angle += ninetydeg;
                for (int j = 0; j < rows; j++)
                {
                    int r = j - hr;
                    float ypos = r * lengthStep;
                    point.x = Radius * Mathf.Cos(angle);  point.y = ypos;  point.z =  Radius * Mathf.Sin(angle);
                    Vector3 org = point - offset;

                    Vector3 toCenter = Vector3.zero - point;
                    toCenter.y = 0;
                    Vector3 dir = toCenter.normalized;

                    rays[j * cols + i].Org = transform.TransformPoint(org);
                    rays[j * cols + i].Dir = transform.TransformDirection(dir);
                }
            }
            return rays;
        }


        public void Start()
        {

            _meshFilter = GetComponent<MeshFilter>();
            _meshFilter.mesh = GenerateCylinderMesh(Radius, Length, WrapDegrees, Subdivisions);

            _avatarLayerMask = 1 << LayerMask.NameToLayer("Avatar");
        }

        private void CancelDelay()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async UniTaskVoid PerformDelay(Action handler)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await UniTask.Delay(_secondsDelay * 1000, cancellationToken: _cancellationTokenSource.Token);
                handler.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("delay cancelled...");
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public void DoSomethingAfterMovementStops()
        {
            Debug.Log("projector tool stopped moving");
            // could set up collision mesh, but probably better to do it after avatar stops moving
        }

        private void DrawDiagnostics(int duration = 0)
        {
            Mesh mesh = _meshFilter.mesh;
            // lower corner pointing out
            Debug.DrawRay(transform.TransformPoint(mesh.vertices[0]), transform.TransformDirection(-1 * mesh.normals[0]), Color.cyan, duration, false);
            // upper corner pointing out
            Debug.DrawRay(transform.TransformPoint(mesh.vertices[mesh.vertices.Length - 1]),
                transform.TransformDirection(-1 * mesh.normals[mesh.vertices.Length - 1]), Color.magenta, duration, false);
            // center pointing out
            Debug.DrawRay(transform.position, transform.forward, Color.yellow, duration, false);
            // cylinder center up
            Debug.DrawRay(CylinderCenter, transform.up, Color.yellow, duration, false);
        }

        public void Update()
        {
#if NOTUSED
        // if projector tool has moved, kick off a countdown, but cancel and restart if
        // still moving - kick off raycast _secondsDelay after movement stops
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            CancelDelay();
            PerformDelay(() =>  DoSomethingAfterMovementStops()).Forget();
        }
#endif
            DrawDiagnostics();
        }

        public void LateUpdate()
        {
            if (_hasLaunched && _calcRaysJobHandle.IsCompleted)
            {
                _calcRaysJobHandle.Complete();
                _isCalcRaysJobFinished = true;
                _hasLaunched = false;
            }
        }

        private void OnValidate()
        {
            if (_meshFilter == null)
            {
                return;
            }

            _meshFilter.mesh = GenerateCylinderMesh(Radius, Length, WrapDegrees, Subdivisions);
        }
    }
}
