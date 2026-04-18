#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

/// <summary>
/// Automatically injects the NSLocation usage description keys into the
/// generated Xcode Info.plist after every iOS build — no manual editing required.
/// </summary>
public static class iOSPostBuild
{
    [PostProcessBuild(1)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;

        string plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict root = plist.root;

        root.SetString(
            "NSLocationWhenInUseUsageDescription",
            "This app uses your GPS location to show nearby points of interest.");

        root.SetString(
            "NSLocationAlwaysAndWhenInUseUsageDescription",
            "This app uses your GPS location to track nearby points of interest.");

        plist.WriteToFile(plistPath);

        UnityEngine.Debug.Log("[iOSPostBuild] Info.plist location keys injected.");
    }
}
#endif
