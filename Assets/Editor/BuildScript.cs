using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public const string ApkPath = "Builds/Android/FlappyClone.apk";

    [MenuItem("Flappy/4 - Gerar APK")]
    public static void BuildApk()
    {
        AndroidSetup.Apply();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        }

        EditorUserBuildSettings.buildAppBundle = false;
        Directory.CreateDirectory(Path.GetDirectoryName(ApkPath));

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Game.unity" },
            locationPathName = ApkPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Flappy] APK gerado: {ApkPath} ({summary.totalSize / 1048576f:F1} MB)");
        }
        else
        {
            Debug.LogError($"[Flappy] Build falhou: {summary.result} ({summary.totalErrors} erros)");
        }
    }
}
