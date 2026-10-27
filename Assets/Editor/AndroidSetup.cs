using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class AndroidSetup
{
    public const string PackageName = "com.srnando.flappyclone";

    [MenuItem("Flappy/3 - Configurar Android")]
    public static void Apply()
    {
        PlayerSettings.companyName = "SrNando";
        PlayerSettings.productName = "Flappy Clone";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

        // retrato travado
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        // Android: ARM64 + IL2CPP (exigência da Play Store)
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.fullscreenMode = FullScreenMode.FullScreenWindow;

        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

        AssetDatabase.SaveAssets();
        Debug.Log("[Flappy] Player Settings de Android aplicados (" + PackageName + ")");
    }
}
