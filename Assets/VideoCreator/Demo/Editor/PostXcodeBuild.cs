using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class PostXcodeBuild
{
    [PostProcessBuild]
    public static void SetXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS) return;

        var plistPath = pathToBuiltProject + "/Info.plist";
        var plist = new PlistDocument();
        plist.ReadFromString(File.ReadAllText(plistPath));

        var rootDict = plist.root;
        rootDict.SetString("NSPhotoLibraryAddUsageDescription", "Genies Camera will add images and video captured by the user to the Camera Roll.");

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}