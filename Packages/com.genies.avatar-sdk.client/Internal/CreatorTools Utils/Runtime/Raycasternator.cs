using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Genies.Components.CreatorTools.TexturePlacement
{
    /// <summary>
    /// The Raycasternator launches the sequential job chain that produces sampled
    /// uv coordinates for an area covering a target mesh. It implements the RaycastCommand
    /// preparation job, instantiating a RaycastCommand object for each ray. RaycastCommands
    /// are the required input format for the actual raycasting job, which is implemented by
    /// Unity. Another job for results processing is implemented here: this takes the results
    /// of the Unity raycasting job and adds its data to the output texture.
    /// </summary>
    public class Raycasternator : IDisposable
    {
        public (int width, int height) OutputTextureDims { get; set; }
        public byte[] OutputTextureData { get; set; }  // results will be written here
        public NativeArray<Ray> Rays { get; set; }
        public (int width, int height) ProjectorTextureDims { get; set; }
        public NativeArray<Color32> DecalPixels { get; set; }
        public int LayerMask { get; set; }
        public int ColliderInstanceID { get; set; }
        public NativeArray<Vector2> ColliderUvs { get; set; }
        public NativeArray<int> ColliderTriangles { get; set; }

        public event Action HasProjected;

        private JobHandle _raycastJobHandle;
        private JobHandle _resultsJobHandle;
        private Stopwatch _stopwatch = new Stopwatch();
        private Stopwatch _mainthreadBatchStopwatch = new Stopwatch();
        private bool _hasLaunched = false;

        public void SubscribeToResultNotification(Action handler)
        {
            HasProjected += handler;
        }

        [BurstCompile]
        public struct BuildRayCastCommandsJob : IJobParallelFor
        {
            public NativeArray<RaycastCommand> Commands;
            public NativeArray<int> DecalIndices;  // indices of non-zero decal pixels
            [ReadOnly]
            public NativeArray<Ray> Rays;
            [ReadOnly]
            public NativeArray<Color32> DecalPixels;  // all the pixels of the projector texture
            [ReadOnly]
            public QueryParameters QueryParameters;
            [ReadOnly]
            public float MaxDistance;
            public void Execute(int index)
            {
                if (DecalPixels[index].a > 0)
                {
                    var ray = Rays[index];
                    Commands[index] = new RaycastCommand(ray.Org, ray.Dir, QueryParameters, MaxDistance);
                    DecalIndices[index] = index;
                }
                else
                {
                    var ray = Rays[index];
                    Commands[index] = new RaycastCommand(ray.Org, ray.Dir, QueryParameters, 0f);
                    DecalIndices[index] = -1;
                }
            }
        }

        [BurstCompile]
        public struct ProcessResultsJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> OutputData;
            [ReadOnly]
            public NativeArray<RaycastHit> Results;
            [ReadOnly]
            public NativeArray<int> DecalIndices;
            [ReadOnly]
            public NativeArray<Color32> DecalPixels;  // all the pixels of the projector texture
            [ReadOnly]
            public int Width;
            [ReadOnly]
            public int Height;
            [ReadOnly]
            public int ColliderInstanceID;
            [ReadOnly]
            public NativeArray<Vector2> ColliderUvs;
            [ReadOnly]
            public NativeArray<int> ColliderTriangles;

            public void Execute(int index)
            {
                int decalIndex = DecalIndices[index];
                // use colliderInstanceId because you can't access any time of game object, component, or any
                // type of reference object inside job
#if UNITY_6000_3_OR_NEWER
                if (Results[index].colliderEntityId == ColliderInstanceID && decalIndex > 0)
#else
                if (Results[index].colliderInstanceID == ColliderInstanceID && decalIndex > 0)
#endif
                {
                    // Results[index].textureCoord cannot be used within a job b/c Unity accesses it via
                    // collider game object - luckily, the triangle index and barycentric coordinates are valid
                    // so we can calculate the textureCoord manually
                    int triIdx = Results[index].triangleIndex;
                    int idx1 = ColliderTriangles[triIdx * 3];
                    int idx2 = ColliderTriangles[triIdx * 3 + 1];
                    int idx3 = ColliderTriangles[triIdx * 3 + 2];
                    Vector3 bc = Results[index].barycentricCoordinate;
                    Vector2 uv = bc.x * ColliderUvs[idx1] + bc.y * ColliderUvs[idx2] + bc.z * ColliderUvs[idx3];
                    float tx = uv.x * (float)Width;
                    float ty = uv.y * (float)Height;
                    Color32 decalColor = DecalPixels[index];
                    int txi = Mathf.RoundToInt(tx);
                    int tyi = Mathf.RoundToInt(ty);
                    OutputData[(tyi * Width * 4) + (txi * 4) + 0] = Premultiply(decalColor.r, decalColor.a);
                    OutputData[(tyi * Width * 4) + (txi * 4) + 1] = Premultiply(decalColor.g, decalColor.a);
                    OutputData[(tyi * Width * 4) + (txi * 4) + 2] = Premultiply(decalColor.b, decalColor.a);
                    OutputData[(tyi * Width * 4) + (txi * 4) + 3] = decalColor.a;
                }
            }
        }

        private static byte Float2Byte(float f)
        {
            //return (byte)Math.Floor(f >= 1.0 ? 255 : f * 256.0);
            return (byte)Mathf.Round(Mathf.Clamp01(f) * 255f);
        }

        // the standard for PNG is unpremultiplied https://www.w3.org/TR/png-3/#6AlphaRepresentation
        // so let's assumme this and premultiply (TODO: this is more efficiently handled in shader)
        private static byte Premultiply(byte colorChannel, byte alphaChannel)
        {
            if (alphaChannel == 255)
            {
                return colorChannel;
            }

            float col = (float)colorChannel * (float)alphaChannel / 255f;
            return (byte)Mathf.Round(col);
        }

        private NativeArray<RaycastCommand> _raycastCommands;
        private NativeArray<int> _decalIndices;
        private NativeArray<RaycastHit> _results;
        private NativeArray<byte> _outputData;

        private void PostJobCleanup()
        {
            // clean up everything this class allocates, plus three arrays it doesn't allocate
            // which are not re-used between job chain runs
            _raycastCommands.Dispose();
            Rays.Dispose();  // allocated by CylinderMeshGenerator
            _decalIndices.Dispose();
            // DecalPixels is cleaned up by Tattooenator when the decal image changes (can be reused for  re-projections)
            ColliderUvs.Dispose();   // allocated by Tattooenator
            ColliderTriangles.Dispose();  // allocated by Tattooenator
            _results.Dispose();
            _outputData.Dispose();
        }

        /// <summary>
        /// Launches three jobs sequentially - each job will launch as soon as its dependency
        /// finishes.
        /// </summary>
        public void LaunchJobChain()
        {
            _stopwatch.Start();
            int size = ProjectorTextureDims.width * ProjectorTextureDims.height;
            _raycastCommands = new NativeArray<RaycastCommand>(size, Allocator.Persistent);
            _decalIndices = new NativeArray<int>(size, Allocator.Persistent);
            var buildCommandsJob = new BuildRayCastCommandsJob()
            {
                Commands = _raycastCommands,
                DecalIndices = _decalIndices,
                Rays = Rays,
                DecalPixels = DecalPixels,
                QueryParameters = new QueryParameters
                {
                    layerMask = LayerMask,
                    hitTriggers = QueryTriggerInteraction.UseGlobal,
                },
                MaxDistance = Mathf.Infinity
            };
            int commandsPerJob = Mathf.Max(size / JobsUtility.JobWorkerCount, 1);
            var buildCommandsJobHandle = buildCommandsJob.Schedule(size, commandsPerJob);

            _results = new NativeArray<RaycastHit>(size, Allocator.Persistent);
            _raycastJobHandle = RaycastCommand.ScheduleBatch(_raycastCommands, _results, commandsPerJob, buildCommandsJobHandle);
            _hasLaunched = true;

            _outputData = new NativeArray<byte>(OutputTextureData, Allocator.Persistent);
            var processResultsJob = new ProcessResultsJob()
            {
                OutputData = _outputData,
                Results = _results,
                DecalIndices = _decalIndices,
                DecalPixels = DecalPixels,
                Width = OutputTextureDims.width,
                Height = OutputTextureDims.height,
                ColliderInstanceID = ColliderInstanceID,
                ColliderUvs = ColliderUvs,
                ColliderTriangles = ColliderTriangles
            };
            _resultsJobHandle = processResultsJob.Schedule(size, commandsPerJob, _raycastJobHandle);
        }

        private async UniTask ScheduleProcessResultsOnMainAsync(int maxFrameTime = -1)
        {
            if (maxFrameTime < 16)
            { // if unspecified or unreasonable, set to platform specific target frame duration
                maxFrameTime = (int)(1000f / (float)Screen.currentResolution.refreshRateRatio.value);
            }
            int width = OutputTextureDims.width, height = OutputTextureDims.height;
            // don't try to iterate through native arrays, it can take 6-7X more time (timed on dbg windows)
            // convert to managed array instead
            RaycastHit[] results = _results.ToArray();
            int[] decalIndices = _decalIndices.ToArray();
            int index = 0;

            _mainthreadBatchStopwatch.Start();
            while (index < results.Length)
            {
                UnityEngine.Profiling.Profiler.BeginSample("Raycastenator.ProcessResultsPerFrame");
                _mainthreadBatchStopwatch.Restart();
                int remainingFrameBudget = maxFrameTime*2 - (int)(Time.deltaTime * 1000);
                //while ((Time.deltaTime * 1000) < maxFrameTime && index < results.Length)
                int startingIndex = index;
                while (_mainthreadBatchStopwatch.ElapsedMilliseconds < remainingFrameBudget && index < results.Length)
                {
                    ProcessSingleResult(index, results, decalIndices);
                    index++;
                }
                _mainthreadBatchStopwatch.Stop();
                UnityEngine.Debug.Log($"<color=yellow>processed {index - startingIndex} results within budget {remainingFrameBudget} elapsed {_mainthreadBatchStopwatch.ElapsedMilliseconds} </color>");
                //_mainthreadBatchStopwatch.Reset();
                //UnityEngine.Debug.Log($"DeltaTime: {Time.deltaTime * 1000.0f} (Application target framerate: {1000.0f / Application.targetFrameRate})");
                UnityEngine.Profiling.Profiler.EndSample();
                await UniTask.Yield();
            }

            _stopwatch.Stop();
            UnityEngine.Debug.Log($"<color=magenta> raycast total elapsed time: {_stopwatch.ElapsedMilliseconds} ms</color>");
            // all done - results are now available in OutputTextureData byte array
            HasProjected?.Invoke();
            PostJobCleanup();
        }

        private void ProcessSingleResult(int index, in RaycastHit[] results, in int[] decalIndices)
        {
#if UNITY_6000_3_OR_NEWER
            if (results[index].colliderEntityId == ColliderInstanceID && decalIndices[index] > 0)
#else
            if (results[index].colliderInstanceID == ColliderInstanceID && decalIndices[index] > 0)
#endif
            {
                int width = OutputTextureDims.width, height = OutputTextureDims.height;
                Vector2 uv = results[index].textureCoord;
                float tx = uv.x * (float)width;
                float ty = uv.y * (float)height;
                Color32 decalColor = DecalPixels[index];
                int txi = Mathf.RoundToInt(tx);
                int tyi = Mathf.RoundToInt(ty);
                OutputTextureData[(tyi * width * 4) + (txi * 4) + 0] = decalColor.r;
                OutputTextureData[(tyi * width * 4) + (txi * 4) + 1] = decalColor.g;
                OutputTextureData[(tyi * width * 4) + (txi * 4) + 2] = decalColor.b;
                OutputTextureData[(tyi * width * 4) + (txi * 4) + 3] = decalColor.a;
            }
        }

        private void Saveout(Texture2D tex)
        {
            var path = $"{Application.streamingAssetsPath}/{tex.name}.png";
            File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));
        }

        // Call this method in LateUpdate
        public void CheckForJobCompletion()
        {
            // wait for chained jobs to complete by checking IsCompleted flag
            // of final job handle - then, before results can be used, Complete()
            // must be called (note that if you do it right away in Update, you'll lock
            // up Unity)
            if (_hasLaunched && _resultsJobHandle.IsCompleted)
            {
                _resultsJobHandle.Complete();
                _hasLaunched = false;

                // job chain is finished (raycommand building, raycasting, and results processing)
                // results are now available in OutputTextureData byte array
                _stopwatch.Stop();
                UnityEngine.Debug.Log($"<color=magenta>    raycast elapsed time: {_stopwatch.ElapsedMilliseconds} ms</color>");
                OutputTextureData = _outputData.ToArray();
                HasProjected?.Invoke();
                PostJobCleanup();
            }
        }

        private void CleanupSubscribers()
        {
            var subscribers = HasProjected?.GetInvocationList();
            if (subscribers != null)
            {
                for (int i = 0; i < subscribers.Length; i++)
                {
                    HasProjected -= subscribers[i] as Action;
                }
            }
        }

        public void Dispose()
        {
            CleanupSubscribers();
        }
    }
}
