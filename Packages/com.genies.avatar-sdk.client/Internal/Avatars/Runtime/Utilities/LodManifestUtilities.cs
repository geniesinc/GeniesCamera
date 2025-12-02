using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Genies.Avatars
{
    public static class LodManifestUtilities
    {
        private const float BaseScreenRelativeTransitionHeight = 0.6f;
        
        private static readonly Comparison<AvatarLodInfo> LodInfoComparison =
            (x, y) => y.TriangleCount.CompareTo(x.TriangleCount);
        
        public static async UniTask<GenieGltfImporter.LodGroupSource> GenerateLodGroupSourceAsync(string lodManifestUrl)
        {
            LodManifestResponse response = await LoadLodManifestAsync(lodManifestUrl);
            if (!response.Success)
            {
                return new GenieGltfImporter.LodGroupSource
                {
                    lods = new List<GenieGltfImporter.LodSource>(0)
                };
            }
            
            return GenerateLodGroupSource(response.Result, response.BaseUrl);
        }

        public static GenieGltfImporter.LodGroupSource GenerateLodGroupSource(string lodManifestJson, string baseUrl = null)
        {
            List<AvatarLodInfo> lodInfos = GetLodInfoFromManifest(lodManifestJson, baseUrl);
            GenieGltfImporter.LodGroupSource lodGroupSource = GenerateLodGroupSource(lodInfos);
            return lodGroupSource;
        }

        public static GenieGltfImporter.LodGroupSource GenerateLodGroupSource(IEnumerable<AvatarLodInfo> lodInfos)
        {
            var lods = new List<AvatarLodInfo>(lodInfos);
            var lodGroupSource = new GenieGltfImporter.LodGroupSource
            {
                lods = new List<GenieGltfImporter.LodSource>(lods.Count)
            };
            
            if (lods.Count == 0)
            {
                return lodGroupSource;
            }

            // sort lods by triangle count (decreasing order)
            lods.Sort(LodInfoComparison);
            
            // add first lod manually with the base screen relative transition height
            lodGroupSource.lods.Add(new GenieGltfImporter.LodSource
            {
                url                            = lods[0].Url,
                screenRelativeTransitionHeight = BaseScreenRelativeTransitionHeight,
                fadeTransitionWidth            = 0.0f,
            });

            for (int i = 1; i < lods.Count; ++i)
            {
                float previousTriangleCount = lods[i - 1].TriangleCount;
                float currentTriangleCount = lods[i].TriangleCount;
                float previousTransitionHeight = lodGroupSource.lods[i - 1].screenRelativeTransitionHeight;
                
                // calculate the transition height for this LOD
                float currentTransitionHeight = previousTransitionHeight * (currentTriangleCount / previousTriangleCount);
                
                lodGroupSource.lods.Add(new GenieGltfImporter.LodSource
                {
                    url = lods[i].Url,
                    screenRelativeTransitionHeight = currentTransitionHeight,
                    fadeTransitionWidth = 0.0f,
                });
            }
            
            // set the lowest LOD transition height to 0 to avoid culling
            int lowestLodIndex = lodGroupSource.lods.Count - 1;
            GenieGltfImporter.LodSource lowestLod = lodGroupSource.lods[lowestLodIndex];
            lowestLod.screenRelativeTransitionHeight = 0.0f;
            lodGroupSource.lods[lowestLodIndex] = lowestLod;
            
            return lodGroupSource;
        }

        public static async UniTask<List<AvatarLodInfo>> LoadLodInfoFromManifestAsync(string lodManifestUrl)
        {
            var results = new List<AvatarLodInfo>();
            await LoadLodInfoFromManifestAsync(lodManifestUrl, results);
            return results;
        }

        public static async UniTask LoadLodInfoFromManifestAsync(string lodManifestUrl, ICollection<AvatarLodInfo> results)
        {
            if (results is null)
            {
                return;
            }

            LodManifestResponse response = await LoadLodManifestAsync(lodManifestUrl);
            if (!response.Success)
            {
                return;
            }

            GetLodInfoFromManifest(response.Result, results, response.BaseUrl);
        }

        public static List<AvatarLodInfo> GetLodInfoFromManifest(string lodManifestJson, string baseUrl = null)
        {
            var results = new List<AvatarLodInfo>();
            GetLodInfoFromManifest(lodManifestJson, results, baseUrl);
            return results;
        }
        
        public static void GetLodInfoFromManifest(string lodManifestJson, ICollection<AvatarLodInfo> results, string baseUrl = null)
        {
            if (results is null)
            {
                return;
            }

            Uri baseUri = baseUrl is null ? null : new Uri(baseUrl);
            LodManifest manifest;

            try
            {
                manifest = JsonConvert.DeserializeObject<LodManifest>(lodManifestJson);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(LodManifestUtilities)}] exception thrown when deserializing the LOD manifest json:\n{exception}");
                return;
            }
            
            if (manifest.lods is null || manifest.lods.Count == 0)
            {
                return;
            }

            for (int i = 0; i < manifest.lods.Count; ++i)
            {
                var info = new AvatarLodInfo
                {
                    Index         = i,
                    Name          = manifest.lods[i].name,
                    TriangleCount = manifest.lods[i].report.render.triangleCount,
                    Url           = GenerateUrl(baseUri, manifest.lods[i].file)
                };
                
                results.Add(info);
            }
        }

        public static string CreateManifestJson(LodGenerateSettings settings)
        {
            LodManifest manifest;

            manifest.lods = new List<Lod>();

            foreach (LodSettings setting in settings.lods)
            {
                var lod = new Lod
                {
                    name = setting.name,
                    file = Path.GetRelativePath(settings.lodRoot, setting.filePath),
                    report = JsonUtility.FromJson<Report>(File.ReadAllText(setting.reportPath)),
                };
                
                manifest.lods.Add(lod);
            }

            return JsonUtility.ToJson(manifest);
        }
        
        public static async UniTask<LodManifestResponse> LoadLodManifestAsync(string lodManifestUrl)
        {
            var uri = new Uri(lodManifestUrl);
            using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            await webRequest.SendWebRequest();
            if (webRequest.result is not UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[{nameof(LodManifestUtilities)}] failed to load LOD manifest from URL: {lodManifestUrl}. Error: {webRequest.error}");
                return default;
            }
            
            // remove the manifest json file from the url to obtain the base URL, so we can get the relative file URLs from the manifest
            string[] uriSegments = uri.Segments;
            string baseUrl = string.Join(string.Empty, uriSegments.Skip(1).Take(uriSegments.Length - 2));
            string authorityUrl = uri.GetLeftPart(UriPartial.Authority);
            
            // this happens when we have a normal URL but not when we have a path within the local file system
            if (!string.IsNullOrEmpty(authorityUrl))
            {
                baseUrl = $"{authorityUrl}/{baseUrl}";
            }

            return new LodManifestResponse
            {
                Success = true,
                Result  = webRequest.downloadHandler.text,
                BaseUrl = baseUrl,
            };
        }

        private static string GenerateUrl(Uri baseUri, string relativeUri)
        {
            return baseUri is null ? relativeUri : new Uri(baseUri, relativeUri).ToString();
        }

        public struct LodManifestResponse
        {
            public bool   Success;
            public string Result; // the manifest as a json string
            public string BaseUrl; // the URL used to load the manifest without the manifest file ending
        }

        [Serializable]
        private struct LodManifest
        {
            public List<Lod> lods;
        }

        [Serializable]
        private struct Lod
        {
            public string name;
            public string file;
            public Report report;
        }

        [Serializable]
        private struct Report
        {
            public Render render;
        }

        [Serializable]
        private struct Render
        {
            public int triangleCount;
        }
    }
}