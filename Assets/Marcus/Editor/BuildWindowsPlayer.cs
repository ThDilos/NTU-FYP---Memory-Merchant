using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWindowsPlayer
{
    [MenuItem("Tools/Memory Merchant/Build Windows Game")]
    public static void Build()
    {
        string directory = Environment.GetEnvironmentVariable("MEMORY_MERCHANT_BUILD_DIR");
        if (string.IsNullOrWhiteSpace(directory)) directory = "Builds/Windows-" + DateTime.Now.ToString("yyyy-MM-dd");
        directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(directory);
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || !scenes[0].EndsWith("Start Menu.unity", StringComparison.Ordinal))
            throw new InvalidOperationException("The Start Menu must be the first enabled build scene.");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(directory, "MemoryMerchant.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        File.WriteAllText(Path.Combine(directory, "Build-Report.txt"),
            "Memory Merchant — Windows x64\nResult: " + report.summary.result +
            "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings +
            "\nSize in bytes: " + report.summary.totalSize + "\nScenes:\n" + string.Join("\n", scenes) + "\n");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        Debug.Log("MEMORY_MERCHANT_BUILD_SUCCEEDED " + directory);
    }
}
