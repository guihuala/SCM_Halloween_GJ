using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuild
{
    [MenuItem("Tools/Build/WebGL 1270x720 Minimal")]
    public static void Build()
    {
        PlayerSettings.defaultWebScreenWidth = 1270;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.template = "APPLICATION:Minimal";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        AssetDatabase.SaveAssets();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled build scenes.");
        Directory.CreateDirectory("Builds/WebGL");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        File.WriteAllText("Builds/WebGL-build-result.txt",
            $"{report.summary.result}\nResolution: 1270x720\nTemplate: Minimal\n" +
            $"Errors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n" +
            $"Bytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\n");
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("WebGL build failed: " + report.summary.result);
        Debug.Log("WEBGL_BUILD_SUCCEEDED: Builds/WebGL (1270x720, Minimal)");
    }
}
