using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;

namespace Genies.Avatars
{
    /// <summary>
    /// Static class for generating LODs from gltf/glb baked avatar
    /// </summary>
    public static class GenieLodGenerator
    {
        public static async UniTask<bool> GenerateLodsAsync(LodGenerateSettings lodGenerateSettings, string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;

            string platform;

            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    platform = "windows";
                    startInfo.FileName = Path.Combine(Application.temporaryCachePath, "gltfpack.exe");
                    break;
                case RuntimePlatform.LinuxPlayer:
                    platform = "ubuntu";
                    startInfo.FileName = Path.Combine(Application.temporaryCachePath, "gltfpack");
                    break;
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    platform = "macos";
                    startInfo.FileName = Path.Combine(Application.temporaryCachePath, "gltfpack");
                    break;
                default:
                    UnityEngine.Debug.LogError($"[{nameof(GenieLodGenerator)}] LODs generatation is only supported on Windows and Linux and MacOS.");
                    return false;
            }

            if (!File.Exists(startInfo.FileName))
            {
                UnityEngine.Debug.Log($"[{nameof(GenieLodGenerator)}] Missing gltfpack executable. Attempting to download from source.");
                var success = await DownloadExecutableAsync(platform, startInfo.FileName);
                if (!success)
                {
                    return false;
                }
            }

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);
            string lodRoot = Path.Join(directory, Path.GetFileNameWithoutExtension(filePath));

            Directory.CreateDirectory(lodRoot);

            await using (UniTask.ReturnToCurrentSynchronizationContext())
            {
                await UniTask.SwitchToThreadPool();

                List<Process> processes = new List<Process>();

                int i = 0;
                foreach (LodSettings setting in lodGenerateSettings.lods)
                {
                    setting.filePath = Path.Join(lodRoot, string.Format("lod{0}", i++), fileName);
                    var process = GenerateLod(startInfo, setting, filePath);
                    if (!lodGenerateSettings.runInParallel)
                    {
                        process.WaitForExit();
                    }

                    processes.Add(process);
                }

                bool success = true;
                foreach (Process process in processes)
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        UnityEngine.Debug.LogError($"[{nameof(GenieLodGenerator)}] Failed to generate LOD: {process.StandardError.ReadToEnd()}");
                        success = false;
                    }
                    process.Dispose();
                }

                if (!success)
                {
                    return false;
                }
            }

            lodGenerateSettings.lodRoot = lodRoot;

            string manifestPath = Path.Join(lodRoot, "/lod_manifest.json");

            File.WriteAllText(manifestPath, LodManifestUtilities.CreateManifestJson(lodGenerateSettings));

            return true;
        }

        private static Process GenerateLod(ProcessStartInfo info, LodSettings lodSettings, string source)
        {
            string dir = Path.GetDirectoryName(lodSettings.filePath);
            Directory.CreateDirectory(dir);
            lodSettings.reportPath = Path.Join(dir, "report.json");

            string compressArg = lodSettings.compression switch
            {
                LodTextureCompression.UASTC => "tu",
                LodTextureCompression.ETC1S => "tc",
                LodTextureCompression.MIXED => "tc -tu normal",
                _ => "tc",
            };

            info.Arguments = string.Format("-{5} -tp -ts \"{0}\" -si \"{1}\" -cc -ke -kn -r \"{2}\" -i \"{3}\" -o \"{4}\"",
                lodSettings.textureRatio, lodSettings.meshRatio, lodSettings.reportPath, source, lodSettings.filePath, compressArg);
            return Process.Start(info);
        }

        private static async UniTask<bool> DownloadExecutableAsync(string platform, string savePath)
        {
            string uri = "https://github.com/zeux/meshoptimizer/releases/latest/download/gltfpack-{0}.zip";
            UnityWebRequest www = new UnityWebRequest(string.Format(uri, platform));

            string zipPath = Path.Combine(Application.temporaryCachePath, "gltfpack.zip");
            www.downloadHandler = new DownloadHandlerFile(zipPath);
            await www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError($"[{nameof(GenieLodGenerator)}] gltfpack download failed: {www.error}");
                return false;
            }
            else
            {
                ZipFile.ExtractToDirectory(zipPath, Path.GetDirectoryName(savePath));
                // set permissions linux / mac
                if (platform is "ubuntu" or "macos")
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x {savePath}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    using var process = new Process();

                    process.StartInfo = startInfo;
                    process.Start();

                    await UniTask.RunOnThreadPool(() => process.WaitForExit());

                    if (process.ExitCode != 0)
                    {
                        var error = await process.StandardError.ReadToEndAsync();
                        UnityEngine.Debug.LogError($"[{nameof(GenieLodGenerator)}] chmod failed: {error}");
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
