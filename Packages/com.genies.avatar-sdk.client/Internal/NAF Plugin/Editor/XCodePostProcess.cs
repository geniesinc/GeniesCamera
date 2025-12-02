#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Genies.NafPlugin.Editor
{
    public class XcodePostProcess
    {
        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
            {
                return;
            }

            // Load the Xcode project
            var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            Debug.Log("Loading Xcode project at: " + projPath);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            // Get Unity’s main target GUID
#if UNITY_2019_3_OR_NEWER
            var frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
            Debug.Log("UnityFramework target GUID = " + frameworkTargetGuid);
#else
            // For older Unity versions there's only one target, so fall back:
            var frameworkTargetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            Debug.Log("(Legacy) Unity target GUID = " + frameworkTargetGuid);
#endif
            Debug.Log("Unity iOS target GUID = " + frameworkTargetGuid);

            // Ask Xcode to link against ImageIO.framework (located in the iOS SDK)
            proj.AddFrameworkToProject(frameworkTargetGuid, "ImageIO.framework", false);

            // Save the modified project file
            proj.WriteToFile(projPath);
            Debug.Log("Wrote modifications to Xcode project. OnPostprocessBuild complete.");

        }
    }
}
#endif
