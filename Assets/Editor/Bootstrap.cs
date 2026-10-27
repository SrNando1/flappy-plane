using UnityEditor;
using UnityEngine;

/// <summary>
/// Roda arte + som + cena + configuração de Android de uma vez.
/// Também é o ponto de entrada usado pela linha de comando (-executeMethod Bootstrap.RunAll).
/// </summary>
public static class Bootstrap
{
    [MenuItem("Flappy/0 - Gerar tudo (arte + som + cena + Android)")]
    public static void RunAll()
    {
        PlaceholderArt.Generate();
        PlaceholderArt.ConfigureHandDrawn();
        PlaceholderAudio.Generate();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        SceneBuilder.Build();
        AndroidSetup.Apply();
        AssetDatabase.SaveAssets();
        Debug.Log("[Flappy] Setup completo.");
    }
}
