using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Renderiza a cena para um PNG, com dois obstáculos posicionados à mão,
/// só para conferir enquadramento e layout sem abrir o Editor.
/// </summary>
public static class SceneShot
{
    public static string OutputPath = "preview.png";

    [MenuItem("Flappy/9 - Salvar preview PNG")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);

        var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Settings/GameConfig.asset");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ObstaclePair.prefab");

        // os dois primeiros prédios da roda, como saem numa partida
        var towers = SceneBuilder.LoadTowerSprites();
        PreviewObstacle(prefab, config, 0.4f, 0.6f, Tower(towers, 0));
        PreviewObstacle(prefab, config, 3.9f, -1.4f, Tower(towers, 1));

        var cam = Camera.main;

        // sem isso a UI (Screen Space Overlay) não aparece na RenderTexture
        var canvas = Object.FindFirstObjectByType<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;

        const int w = 540, h = 960;
        var rt = new RenderTexture(w, h, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;

        File.WriteAllBytes(OutputPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        rt.Release();

        Debug.Log("[Flappy] Preview salvo em " + Path.GetFullPath(OutputPath));
    }

    static void PreviewObstacle(GameObject prefab, GameConfig config, float x, float gapCenterY, Sprite tower)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.position = new Vector3(x, 0f, 0f);
        go.GetComponent<ObstaclePair>().Init(config, gapCenterY, tower);
    }

    static Sprite Tower(Sprite[] towers, int index)
    {
        if (towers.Length == 0) return null;
        return towers[index % towers.Length];
    }
}
