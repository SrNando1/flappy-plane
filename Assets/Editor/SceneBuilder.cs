using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta a cena inteira do jogo por código: câmera, avião, chão, obstáculos,
/// prefab do par torre+relâmpago e toda a UI. Rode de novo para regerar do zero.
/// </summary>
public static class SceneBuilder
{
    const string ScenePath = "Assets/Scenes/Game.unity";
    const string PrefabPath = "Assets/Prefabs/ObstaclePair.prefab";
    const string ConfigPath = "Assets/Settings/GameConfig.asset";
    const string CatalogPath = "Assets/Settings/PlaneCatalog.asset";

    /// <summary>Quanto custa o A320 na loja, em moedas.</summary>
    const int A320Price = 200;

    // som de fundo: vem de fora, não é gerado pelo PlaceholderAudio
    const string MusicClip = "background_sound_rain";

    const float GroundTopY = -3.88f;
    const float GroundTileW = 6.72f;
    const float GroundTileH = 1.12f;
    const float BushTileH = 0.96f;
    const float CloudTopY = 5.4f;    // um pouco acima do topo do ecrã (5)

    /// <summary>Como começa o nome da peça do chapéu, na prancheta.</summary>
    const string HatPart = "Chapeu";

    /// <summary>
    /// O número do prédio que traz estendal. É o do desenho: o estendal foi
    /// desenhado em cima do prédio 4, com o telhado dele por baixo.
    /// </summary>
    const string ClotheslineTowerNumber = "4";

    [MenuItem("Flappy/2 - Montar cena do jogo")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // depois do NewScene: criar a cena descarrega assets sem referências
        // e invalidaria uma instância obtida antes daqui
        var config = LoadOrCreateConfig();

        BuildCamera();
        var manager = BuildManager(config, BuildPlaneCatalog());
        BuildBackground(config);
        BuildRain();

        // a UI vem antes porque o clarão de ecrã inteiro vive nela
        // e os efeitos de queda precisam de uma referência para ele
        var flash = BuildUI();

        // os prédios vêm antes do crash: é a mesma instância de Sprite que tem
        // de chegar aos dois, senão o desabamento não reconhece o prédio 1
        var towers = LoadTowerSprites();
        var crash = BuildCrashEffects(config, flash, towers);
        BuildAudio(config);
        BuildAds();

        var plane = BuildAirplane(config, crash);
        var prefab = BuildObstaclePrefab(config, towers);
        BuildSpawner(config, prefab, towers, plane);

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("[Flappy] Cena montada em " + ScenePath + " (GameManager: " + manager.name + ")");
    }

    // ---------------------------------------------------------------- pedaços

    static GameConfig LoadOrCreateConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        if (config != null) return config;

        Directory.CreateDirectory("Assets/Settings");
        config = ScriptableObject.CreateInstance<GameConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);
        AssetDatabase.SaveAssets();
        return config;
    }

    static void BuildCamera()
    {
        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0f, 0f, -10f);

        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        // céu de dia de chuva: o mesmo azul de sempre, com metade da luz
        // (#4EC0CA → #24606E, o tom e a saturação quase iguais, o brilho a meio)
        cam.backgroundColor = new Color32(36, 96, 110, 255);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 50f;

        go.AddComponent<AudioListener>();
        go.AddComponent<CameraShake>();
    }

    static CrashEffects BuildCrashEffects(GameConfig config, ScreenFlash flash, Sprite[] towers)
    {
        var go = new GameObject("CrashEffects");
        var fx = go.AddComponent<CrashEffects>();

        SetRef(fx, "config", config);
        SetRef(fx, "flash", flash);
        SetRef(fx, "spark", LoadSprite("spark"));
        SetRef(fx, "ring", LoadSprite("ring"));

        // o chapéu sai da lista dos destroços: não voa com o resto, cai
        // sozinho no fim (ver CrashEffects.HatDrop)
        var parts = new List<Sprite>(LoadPlaneParts());
        SetRef(fx, "hat", TakeHat(parts));
        SetRefArray(fx, "planeParts", parts.ToArray());

        // tem de ser a mesma instância que vai parar aos obstáculos: o
        // desabamento reconhece o prédio por referência, não por nome
        SetTowerDebris(fx, towers);

        return fx;
    }

    static GameObject BuildManager(GameConfig config, PlaneCatalog planes)
    {
        var go = new GameObject("GameManager");
        var manager = go.AddComponent<GameManager>();
        SetRef(manager, "config", config);
        SetRef(manager, "planes", planes);
        return go;
    }

    /// <summary>
    /// Os aviões da loja. Reescrito a cada montagem, como o resto da cena: um
    /// preço mexido à mão no asset volta ao que está aqui. As frações foram
    /// medidas no próprio desenho — ver <see cref="PlaneSkin"/>.
    /// </summary>
    static PlaneCatalog BuildPlaneCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<PlaneCatalog>(CatalogPath);
        if (catalog == null)
        {
            Directory.CreateDirectory("Assets/Settings");
            catalog = ScriptableObject.CreateInstance<PlaneCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var planes = new List<PlaneSkin>
        {
            // o de origem: as frações por omissão do PlaneSkin são as dele
            new PlaneSkin { id = "cessna", displayName = "CESSNA", sprite = LoadSprite("plane"), price = 0 },
        };

        var a320 = LoadOptionalSprite(PlaceholderArt.Dir + "/" + PlaceholderArt.A320Name + ".png", "a loja fica sem o A320");
        if (a320 != null)
        {
            planes.Add(new PlaneSkin
            {
                id = "a320",
                displayName = "AIRBUS A320",
                sprite = a320,
                price = A320Price,
                // só a fuselagem: a deriva ocupa a metade de cima do desenho
                // e bater-lhe com a ponta seria injusto
                colliderSize = new Vector2(0.93f, 0.4f),
                colliderOffset = new Vector2(0f, -0.21f),
                cockpit = new Vector2(0.43f, -0.13f),
                nose = new Vector2(0.48f, -0.26f),
                // a saída do reator, debaixo da asa
                nozzle = new Vector2(0.09f, -0.37f),
            });
        }

        catalog.planes = planes.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    static void BuildAds()
    {
        var go = new GameObject("AdsService");

        // canvas próprio: o AdsService sobrevive à recarga da cena e o
        // retângulo de simulação tem de o acompanhar, senão fica pendurado
        var canvasGo = new GameObject("AdCanvas");
        canvasGo.transform.SetParent(go.transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var bar = new GameObject("BannerPlaceholder", typeof(RectTransform));
        bar.transform.SetParent(canvasGo.transform, false);

        var image = bar.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.13f, 0.92f);
        image.raycastTarget = false;

        var rt = image.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, 150f);

        var label = MakeText(bar.transform, "Label", "AD (simulated)", 46, Vector2.zero, new Vector2(1000f, 120f));
        label.color = new Color(0.76f, 0.76f, 0.82f, 1f);

        var ads = go.AddComponent<AdsService>();
        SetRef(ads, "placeholder", bar);
    }

    static void BuildAudio(GameConfig config)
    {
        var go = new GameObject("Audio");

        var sfx = go.AddComponent<AudioSource>();
        sfx.playOnAwake = false;

        // fonte separada porque o motor toca em ciclo com volume e altura próprios
        var engineGo = new GameObject("Engine");
        engineGo.transform.SetParent(go.transform, false);
        var engine = engineGo.AddComponent<AudioSource>();
        engine.clip = LoadClip("avioneta_sound");
        engine.loop = true;
        engine.playOnAwake = false;
        engine.volume = 0f;

        // a chuva: fonte própria, em ciclo, 2D e com prioridade máxima —
        // é o som que nunca pode ser descartado quando há muitas vozes juntas
        var musicGo = new GameObject("Music");
        musicGo.transform.SetParent(go.transform, false);
        var music = musicGo.AddComponent<AudioSource>();
        music.clip = LoadOptionalClip(MusicClip);
        music.loop = true;
        music.playOnAwake = false;
        music.volume = 0f;
        music.spatialBlend = 0f;
        music.priority = 0;

        var manager = go.AddComponent<AudioManager>();
        SetRef(manager, "config", config);
        SetRef(manager, "sfxSource", sfx);
        SetRef(manager, "engineSource", engine);
        SetRef(manager, "musicSource", music);
        SetRef(manager, "score", LoadClip("sfx_score"));
        SetRef(manager, "crash", LoadClip("sfx_crash"));
        SetRef(manager, "start", LoadClip("sfx_start"));
        SetRef(manager, "record", LoadClip("sfx_record"));
        SetRef(manager, "lightning", LoadClip("Relampago_sound"));
        SetRef(manager, "pigeon", LoadClip("sfx_pigeon"));
        SetRef(manager, "coin", LoadClip("sfx_coin"));
    }

    static void BuildBackground(GameConfig config)
    {
        // faixa de nuvens no topo — é de dentro dela que os relâmpagos nascem.
        // Ordem 8: à frente do relâmpago (5), atrás do avião (20), portanto a
        // ponta de cima do raio fica escondida na nuvem.
        //
        // O tamanho vem do próprio sprite: a faixa é arte manual e quem a
        // redesenhar muda a espessura da tempestade sem mexer em código.
        var cloudSprite = LoadSprite("cloud");
        float cloudW = cloudSprite != null ? cloudSprite.bounds.size.x : PlaceholderArt.CloudTileW;
        float cloudH = cloudSprite != null ? cloudSprite.bounds.size.y : 1.76f;

        if (cloudW < 6f)
        {
            Debug.LogWarning("[Flappy] cloud.png dá um tile de " + cloudW.ToString("0.00") +
                             " unidades, estreito demais para os dois tiles cobrirem o ecrã. " +
                             "Rode 'Flappy/1c' para o PPU sair da largura do desenho.");
        }

        var clouds = new GameObject("Clouds");
        clouds.transform.position = new Vector3(0f, CloudTopY - cloudH * 0.5f, 0f);
        AddTiles(clouds.transform, "cloud", cloudW, 8);
        var cloudScroll = clouds.AddComponent<ScrollingTiles>();
        SetFloat(cloudScroll, "speedMultiplier", 0.5f);
        SetFloat(cloudScroll, "tileWidth", cloudW);
        SetBool(cloudScroll, "stopOnGameOver", true);

        // arbustos ao fundo, rolando mais devagar (parallax)
        var bushes = new GameObject("Bushes");
        bushes.transform.position = new Vector3(0f, GroundTopY + BushTileH * 0.5f, 0f);
        AddTiles(bushes.transform, "bushes", GroundTileW, 0);
        var bushScroll = bushes.AddComponent<ScrollingTiles>();
        SetFloat(bushScroll, "speedMultiplier", 0.35f);
        SetFloat(bushScroll, "tileWidth", GroundTileW);
        SetBool(bushScroll, "stopOnGameOver", true);

        // chão
        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0f, GroundTopY - GroundTileH * 0.5f, 0f);
        AddTiles(ground.transform, "ground", GroundTileW, 10);
        var groundScroll = ground.AddComponent<ScrollingTiles>();
        SetFloat(groundScroll, "speedMultiplier", 1f);
        SetFloat(groundScroll, "tileWidth", GroundTileW);
        SetBool(groundScroll, "stopOnGameOver", true);

        // colisor do chão (fixo, não rola)
        var solid = new GameObject("GroundCollider");
        solid.transform.position = new Vector3(0f, GroundTopY - 1.5f, 0f);
        var box = solid.AddComponent<BoxCollider2D>();
        box.size = new Vector2(20f, 3f);
    }

    /// <summary>
    /// Chuva por cima do cenário. É montada aqui e não à mão no Inspector
    /// porque esta cena é regerada do zero a cada 'Flappy/2': o que se liga à
    /// mão desaparece na montagem seguinte.
    ///
    /// As três camadas e a ordem de desenho vivem no próprio Rain — aqui só se
    /// lhe entrega o sprite.
    /// </summary>
    static void BuildRain()
    {
        var drop = LoadDropSprite();
        if (drop == null)
        {
            Debug.LogWarning("[Flappy] Gota não encontrada em " + PlaceholderArt.Dir +
                             " — a cena fica sem chuva. Exporte a gota e rode 'Flappy/1c' " +
                             "para o PNG passar de textura a Sprite.");
            return;
        }

        var rain = new GameObject("Rain");
        var component = rain.AddComponent<Rain>();
        SetRef(component, "drop", drop);
    }

    /// <summary>
    /// A gota costuma chegar com o nome da prancheta do Illustrator, por isso a
    /// procura aceita várias grafias em vez de exigir um nome exacto. Só conta
    /// o PNG que já esteja marcado como Sprite: uma textura simples devolve
    /// null aqui, tal como devolveria no campo do Inspector.
    /// </summary>
    static Sprite LoadDropSprite()
    {
        string[] names = { "raindrop", "gota", "Gota", "Gota-02", "Gota_Prancheta 1" };

        foreach (var name in names)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderArt.Dir + "/" + name + ".png");
            if (sprite != null) return sprite;
        }

        return null;
    }

    static void AddTiles(Transform parent, string spriteName, float tileWidth, int sortingOrder)
    {
        var sprite = LoadSprite(spriteName);

        for (int i = 0; i < 2; i++)
        {
            var tile = new GameObject(spriteName + "_" + i);
            tile.transform.SetParent(parent, false);
            tile.transform.localPosition = new Vector3(tileWidth * i, 0f, 0f);

            var sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
        }
    }

    static Transform BuildAirplane(GameConfig config, CrashEffects crash)
    {
        var go = new GameObject("Airplane");
        go.transform.position = new Vector3(-1.2f, 0.5f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LoadSprite("plane");
        sr.sortingOrder = 20;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // cápsula estreita: cobre a fuselagem, não a hélice nem o leme
        var col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size = FuselageSize(sr.sprite);

        var plane = go.AddComponent<Airplane>();
        SetRef(plane, "config", config);
        SetRef(plane, "crash", crash);

        var smoke = go.AddComponent<EngineSmoke>();
        SetRef(smoke, "puff", LoadSprite("spark"));

        // os olhos do embate: desligados a partida toda, acendem-se na pausa
        var eyesGo = new GameObject("Eyes");
        eyesGo.transform.SetParent(go.transform, false);
        eyesGo.transform.localPosition = CockpitOffset(sr.sprite);

        var eyesSr = eyesGo.AddComponent<SpriteRenderer>();
        eyesSr.sprite = LoadSprite("eyes");
        eyesSr.sortingOrder = sr.sortingOrder + 1;
        eyesSr.enabled = false;
        SetRef(plane, "eyes", eyesSr);

        // a roupa do estendal: sem peça nenhuma até ao primeiro estendal
        var laundry = go.AddComponent<PlaneLaundry>();
        SetRefArray(laundry, "garments", LoadLaundry());
        SetVector2(laundry, "pinOffset", PropellerHub(sr.sprite));

        return go.transform;
    }

    /// <summary>
    /// Onde a roupa fica presa: o cubo da hélice, medido no plane.png (o bico
    /// azul na ponta do nariz). Frações do sprite pelo mesmo motivo do
    /// <see cref="FuselageSize"/>.
    /// </summary>
    static Vector2 PropellerHub(Sprite plane)
    {
        if (plane == null) return new Vector2(0.59f, 0f);

        var size = plane.bounds.size;
        return new Vector2(size.x * 0.47f, size.y * -0.01f);
    }

    /// <summary>
    /// As peças de roupa que podem calhar na hélice, lidas da pasta. Uma sexta
    /// peça exportada para lá entra no sorteio sem mexer aqui.
    /// </summary>
    static Sprite[] LoadLaundry()
    {
        var paths = PlaceholderArt.LaundryPaths();
        var sprites = new List<Sprite>();

        foreach (var path in paths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sprites.Add(sprite);
        }

        if (paths.Length == 0)
        {
            Debug.LogWarning("[Flappy] Nenhuma peça em " + PlaceholderArt.LaundryDir + " — o estendal abana mas não veste a hélice.");
        }
        else if (sprites.Count < paths.Length)
        {
            Debug.LogError("[Flappy] " + (paths.Length - sprites.Count) + " de " + paths.Length + " peças de roupa não são Sprite — " +
                           "rode 'Flappy/1c - Ajustar importação da arte manual' e monte a cena outra vez.");
        }

        return sprites.ToArray();
    }

    /// <summary>
    /// Onde ficam os olhos no avião, tirado do sprite e não de números fixos —
    /// pelo mesmo motivo do <see cref="FuselageSize"/>. As frações apontam para
    /// o vidro da cabina; como os olhos são mais altos que a fuselagem, ficam
    /// meio a sair por cima dela, que é como se desenha um susto.
    /// </summary>
    static Vector3 CockpitOffset(Sprite plane)
    {
        if (plane == null) return new Vector3(0.21f, 0.11f, 0f);

        var size = plane.bounds.size;
        return new Vector3(size.x * 0.17f, size.y * 0.3f, 0f);
    }

    /// <summary>
    /// Tamanho da cápsula do avião, a partir do sprite em vez de números fixos:
    /// assim reexportar a arte noutra proporção não deixa o colisor a flutuar.
    /// As frações apanham a fuselagem e deixam de fora a hélice, o leme e o trem.
    /// </summary>
    static Vector2 FuselageSize(Sprite sprite)
    {
        if (sprite == null) return new Vector2(0.72f, 0.24f);

        var size = sprite.bounds.size;
        return new Vector2(size.x * 0.87f, size.y * 0.41f);
    }

    static GameObject BuildObstaclePrefab(GameConfig config, Sprite[] towers)
    {
        var root = new GameObject("ObstaclePair");
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        root.AddComponent<ObstaclePair>();

        // prédio: pivô no telhado, pendurado na borda do vão. O que se guarda
        // aqui é o primeiro da roda — cada obstáculo troca o seu quando nasce
        CreateTower(root.transform, towers.Length > 0 ? towers[0] : LoadSprite("tower"));

        // raio: pivô na ponta, esticado por código a cada obstáculo
        CreateLightning(root.transform, LoadSprite("lightning"));

        var zone = new GameObject("ScoreZone");
        zone.transform.SetParent(root.transform, false);
        zone.AddComponent<ScoreZone>();
        var trigger = zone.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.2f, config.obstacleGap);

        // estendal: desligado no prefab, cada obstáculo decide se o liga
        CreateClothesline(root.transform, LoadOptionalSprite(
            PlaceholderArt.Dir + "/" + PlaceholderArt.ClotheslineName + ".png", "não vai haver estendais"));

        Directory.CreateDirectory("Assets/Prefabs");
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>
    /// O prédio do obstáculo. Leva colisor em polígono pela mesma razão que o
    /// relâmpago: os prédios têm antena, telhado a sair para fora e cornija, e
    /// uma caixa à volta disso mataria o avião no ar vazio. A forma é recopiada
    /// pelo ObstaclePair a cada obstáculo, do prédio que lhe calhar.
    /// </summary>
    static void CreateTower(Transform parent, Sprite sprite)
    {
        var go = new GameObject("Tower");
        go.transform.SetParent(parent, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;

        go.AddComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// O relâmpago leva colisor em polígono, gerado a partir da própria forma.
    /// Com um retângulo, o avião morria no ar vazio ao lado do ziguezague.
    /// </summary>
    static void CreateLightning(Transform parent, Sprite sprite)
    {
        var go = new GameObject("Lightning");
        go.transform.SetParent(parent, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;

        go.AddComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// O estendal: um trigger, não um colisor. Atravessa-se sem morrer. A caixa
    /// cobre só a zona da roupa, não o estendal inteiro — passar no ar entre a
    /// corda e o telhado sem tocar em pano nenhum e sair de lá vestido lia-se
    /// como avaria.
    /// </summary>
    static void CreateClothesline(Transform parent, Sprite sprite)
    {
        var go = new GameObject("Clothesline");
        go.transform.SetParent(parent, false);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 6;   // à frente do prédio, atrás do avião

        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;

        if (sprite != null)
        {
            // pivô na base: o desenho vai de 0 à altura. A roupa começa logo
            // abaixo da corda e desce até perto de um terço do fundo.
            var size = sprite.bounds.size;
            trigger.size = new Vector2(size.x * 0.9f, size.y * 0.65f);
            trigger.offset = new Vector2(0f, size.y * 0.625f);
        }

        go.AddComponent<Clothesline>();
        go.SetActive(false);
    }

    static void BuildSpawner(GameConfig config, GameObject prefab, Sprite[] towers, Transform plane)
    {
        var go = new GameObject("ObstacleSpawner");
        var spawner = go.AddComponent<ObstacleSpawner>();
        SetRef(spawner, "config", config);
        SetRef(spawner, "obstaclePairPrefab", prefab);
        SetRefArray(spawner, "towerSprites", towers);
        SetRef(spawner, "clotheslineTower", ClotheslineTower(towers));
        SetRef(spawner, "pigeons", BuildPigeons(config, plane));
        SetRef(spawner, "coins", BuildCoins(config));
    }

    /// <summary>
    /// Os pombos: um objeto só, que os larga à medida que os pares nascem.
    /// Sem os fotogramas do pombo fica sem pombos, com aviso — não é por falta
    /// de uma piada que o jogo deixa de correr.
    /// </summary>
    static PigeonSpawner BuildPigeons(GameConfig config, Transform plane)
    {
        var go = new GameObject("Pigeons");
        var pigeons = go.AddComponent<PigeonSpawner>();

        SetRef(pigeons, "config", config);
        SetRef(pigeons, "plane", plane);
        SetRef(pigeons, "wingsUp", LoadOptionalSprite(PlaceholderArt.Dir + "/pigeon_up.png", "não vai haver pombos"));
        SetRef(pigeons, "wingsDown", LoadOptionalSprite(PlaceholderArt.Dir + "/pigeon_down.png", "o pombo não bate as asas"));
        SetRef(pigeons, "feather", LoadOptionalSprite(PlaceholderArt.Dir + "/feather.png", "o pombo não larga penas"));

        return pigeons;
    }

    /// <summary>
    /// As moedas: um objeto só, que as larga empilhadas no vão à medida que os
    /// pares nascem. Sem sprite fica sem moedas, com aviso — não é por falta de
    /// uma moeda que o jogo deixa de correr.
    /// </summary>
    static CoinSpawner BuildCoins(GameConfig config)
    {
        var go = new GameObject("Coins");
        var coins = go.AddComponent<CoinSpawner>();

        SetRef(coins, "config", config);
        SetRef(coins, "coinSprite", LoadOptionalSprite(PlaceholderArt.Dir + "/coin.png", "não vai haver moedas"));
        SetRef(coins, "sparkSprite", LoadSprite("spark"));

        return coins;
    }

    /// <summary>
    /// O prédio do estendal, escolhido pelo número e não pelo nome inteiro:
    /// os prédios chamam-se "predio 1" e "Predio4", com e sem espaço, e o
    /// número é a única parte em que concordam. É o mesmo critério que liga
    /// cada prédio à pasta dos cacos dele.
    /// </summary>
    static Sprite ClotheslineTower(Sprite[] towers)
    {
        foreach (var tower in towers)
        {
            if (tower != null && PlaceholderArt.TowerNumber(tower.name) == ClotheslineTowerNumber) return tower;
        }

        Debug.LogWarning("[Flappy] Nenhum prédio " + ClotheslineTowerNumber + " entre os obstáculos — não vai haver estendais.");
        return null;
    }

    // ------------------------------------------------------------------- UI

    static ScreenFlash BuildUI()
    {
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        var score = MakeText(canvasGo.transform, "ScoreText", "0", 140, new Vector2(0f, -280f), new Vector2(800f, 220f));
        score.rectTransform.anchorMin = score.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        score.rectTransform.pivot = new Vector2(0.5f, 1f);

        // recorde no canto superior esquerdo, sempre à vista durante a partida
        // o letreiro do pombo, logo abaixo do placar: é para onde o olho já
        // vai durante a partida, e não tapa o avião
        var toast = MakeText(canvasGo.transform, "Toast", "", 72, new Vector2(0f, 330f), new Vector2(1000f, 140f));
        toast.color = new Color32(255, 214, 66, 255);
        toast.enabled = false;

        var best = MakeText(canvasGo.transform, "BestText", "BEST  0", 46, new Vector2(44f, -44f), new Vector2(620f, 80f));
        best.alignment = TextAnchor.MiddleLeft;
        best.color = new Color(1f, 1f, 1f, 0.8f);
        best.rectTransform.anchorMin = best.rectTransform.anchorMax = new Vector2(0f, 1f);
        best.rectTransform.pivot = new Vector2(0f, 1f);

        // contador de moedas, a espelhar o recorde no canto oposto
        var coins = MakeText(canvasGo.transform, "CoinsText", "COINS  0", 46, new Vector2(-44f, -44f), new Vector2(620f, 80f));
        coins.alignment = TextAnchor.MiddleRight;
        coins.color = new Color32(255, 214, 66, 255);
        coins.rectTransform.anchorMin = coins.rectTransform.anchorMax = new Vector2(1f, 1f);
        coins.rectTransform.pivot = new Vector2(1f, 1f);
        coins.enabled = false;

        // painel inicial
        var ready = MakePanel(canvasGo.transform, "ReadyPanel");
        MakeText(ready.transform, "Title", "SKYLINE", 120, new Vector2(0f, 520f), new Vector2(900f, 200f));
        var start = MakeButton(ready.transform, "StartButton", "START", new Vector2(0f, -200f));
        start.gameObject.AddComponent<StartButton>();
        var store = MakeButton(ready.transform, "StoreButton", "STORE", new Vector2(0f, -370f));
        // a carteira, por baixo do recorde — filha do painel inicial de propósito:
        // some sozinha assim que a partida começa, sem precisar de mais um SetActive
        var totalCoins = MakeText(ready.transform, "TotalCoins", "TOTAL COINS  0", 40, new Vector2(44f, -128f), new Vector2(620f, 70f));
        totalCoins.alignment = TextAnchor.MiddleLeft;
        totalCoins.color = new Color32(255, 214, 66, 255);
        totalCoins.rectTransform.anchorMin = totalCoins.rectTransform.anchorMax = new Vector2(0f, 1f);
        totalCoins.rectTransform.pivot = new Vector2(0f, 1f);

        var storePanel = BuildStore(canvasGo.transform, ready);

        // painel de fim de jogo
        var over = MakePanel(canvasGo.transform, "GameOverPanel");

        var dimGo = new GameObject("Dim", typeof(RectTransform));
        dimGo.transform.SetParent(over.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.45f);
        Stretch(dim.rectTransform);

        MakeText(over.transform, "Title", "YOU CRASHED", 100, new Vector2(0f, 340f), new Vector2(1000f, 200f));
        var badge = MakeText(over.transform, "RecordBadge", "NEW BEST!", 56, new Vector2(0f, 205f), new Vector2(1000f, 120f));
        badge.color = new Color32(255, 214, 66, 255);
        var final = MakeText(over.transform, "FinalScore", "SCORE  0", 72, new Vector2(0f, 60f), new Vector2(1000f, 140f));
        var high = MakeText(over.transform, "HighScore", "BEST  0", 72, new Vector2(0f, -40f), new Vector2(1000f, 140f));
        var coinsFinal = MakeText(over.transform, "CoinsFinal", "COINS  0   TOTAL  0", 48, new Vector2(0f, -125f), new Vector2(1000f, 100f));
        coinsFinal.color = new Color32(255, 214, 66, 255);
        // a frase de remate, entre os números e o convite a jogar outra vez:
        // é lá que o olho já está quando acaba de ler o score
        var quip = MakeText(over.transform, "Quip", "", 42, new Vector2(0f, -210f), new Vector2(1000f, 120f));
        quip.color = new Color(1f, 1f, 1f, 0.75f);

        MakeText(over.transform, "Hint", "TAP TO PLAY AGAIN", 50, new Vector2(0f, -320f), new Vector2(1000f, 120f));

        over.SetActive(false);

        var ui = canvasGo.AddComponent<UIController>();
        SetRef(ui, "scoreText", score);
        SetRef(ui, "bestText", best);
        SetRef(ui, "coinsText", coins);
        SetRef(ui, "totalCoinsText", totalCoins);
        SetRef(ui, "readyPanel", ready);
        SetRef(ui, "gameOverPanel", over);
        SetRef(ui, "finalScoreText", final);
        SetRef(ui, "highScoreText", high);
        SetRef(ui, "coinsFinalText", coinsFinal);
        SetRef(ui, "recordBadge", badge);
        SetRef(ui, "quipText", quip);
        SetRef(ui, "toastText", toast);
        SetRef(ui, "store", storePanel);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(store.onClick, ui.OnStorePressed);

        // último filho do Canvas: desenha por cima de tudo
        var flashGo = new GameObject("Flash", typeof(RectTransform));
        flashGo.transform.SetParent(canvasGo.transform, false);
        var flashImage = flashGo.AddComponent<Image>();
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false;
        Stretch(flashImage.rectTransform);

        new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));

        return flashGo.AddComponent<ScreenFlash>();
    }

    /// <summary>
    /// A loja de aviões: tapa a tela inicial com um véu escuro e mostra um
    /// avião de cada vez. Fechada até alguém carregar em STORE.
    /// </summary>
    static StorePanel BuildStore(Transform canvas, GameObject ready)
    {
        var panel = MakePanel(canvas, "StorePanel");

        // o véu apanha os toques: nada por baixo da loja reage enquanto ela está aberta
        var dimGo = new GameObject("Dim", typeof(RectTransform));
        dimGo.transform.SetParent(panel.transform, false);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        Stretch(dim.rectTransform);

        MakeText(panel.transform, "Title", "STORE", 110, new Vector2(0f, 640f), new Vector2(900f, 180f));

        var wallet = MakeText(panel.transform, "Wallet", "COINS  0", 56, new Vector2(0f, 500f), new Vector2(900f, 100f));
        wallet.color = new Color32(255, 214, 66, 255);

        var previewGo = new GameObject("Preview", typeof(RectTransform));
        previewGo.transform.SetParent(panel.transform, false);
        var preview = previewGo.AddComponent<Image>();
        preview.preserveAspect = true;
        preview.raycastTarget = false;
        var prt = preview.rectTransform;
        prt.anchoredPosition = new Vector2(0f, 200f);
        prt.sizeDelta = new Vector2(900f, 320f);

        var planeName = MakeText(panel.transform, "PlaneName", "", 72, new Vector2(0f, -20f), new Vector2(1000f, 120f));

        var prev = MakeButton(panel.transform, "PrevButton", "<", new Vector2(-300f, -170f));
        ((RectTransform)prev.transform).sizeDelta = new Vector2(150f, 130f);
        var next = MakeButton(panel.transform, "NextButton", ">", new Vector2(300f, -170f));
        ((RectTransform)next.transform).sizeDelta = new Vector2(150f, 130f);
        var page = MakeText(panel.transform, "Page", "", 50, new Vector2(0f, -170f), new Vector2(400f, 100f));

        var action = MakeButton(panel.transform, "ActionButton", "SELECT", new Vector2(0f, -370f));
        var actionLabel = action.GetComponentInChildren<Text>();

        var hint = MakeText(panel.transform, "Hint", "NOT ENOUGH COINS", 44, new Vector2(0f, -490f), new Vector2(1000f, 90f));
        hint.color = new Color32(255, 110, 90, 255);
        hint.enabled = false;

        var back = MakeButton(panel.transform, "BackButton", "BACK", new Vector2(0f, -640f));

        var store = panel.AddComponent<StorePanel>();
        SetRef(store, "readyPanel", ready);
        SetRef(store, "preview", preview);
        SetRef(store, "nameText", planeName);
        SetRef(store, "walletText", wallet);
        SetRef(store, "pageText", page);
        SetRef(store, "actionButton", action);
        SetRef(store, "actionLabel", actionLabel);
        SetRef(store, "prevButton", prev);
        SetRef(store, "nextButton", next);
        SetRef(store, "hintText", hint);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(prev.onClick, store.Prev);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(next.onClick, store.Next);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(action.onClick, store.OnAction);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(back.onClick, store.Close);

        panel.SetActive(false);
        return store;
    }

    static GameObject MakePanel(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    static Text MakeText(Transform parent, string name, string content, int size, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(4f, -4f);

        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;

        return text;
    }

    /// <summary>
    /// Botão da tela inicial: fundo amarelo, contorno preto de 1 e letras a
    /// preto. O contorno é um Outline no próprio fundo — num retângulo dá o
    /// mesmo que uma moldura, sem mais uma imagem por trás.
    /// </summary>
    static Button MakeButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = new Color32(255, 214, 66, 255);

        var border = go.AddComponent<Outline>();
        border.effectColor = Color.black;
        border.effectDistance = new Vector2(1f, -1f);

        var rt = image.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(520f, 130f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var text = MakeText(go.transform, "Label", label, 64, Vector2.zero, rt.sizeDelta);
        text.color = Color.black;
        Object.DestroyImmediate(text.GetComponent<Outline>());

        return button;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // -------------------------------------------------------------- utilidades

    /// <summary>
    /// Como o <see cref="LoadSprite"/>, mas para arte de que o jogo não precisa
    /// para correr: sem ela há um aviso e uma funcionalidade a menos, não um erro.
    /// </summary>
    static Sprite LoadOptionalSprite(string path, string consequence)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogWarning("[Flappy] " + path + " não existe ou não é Sprite — " + consequence +
                             ". Se o ficheiro lá está, rode 'Flappy/1c' antes.");
        }
        return sprite;
    }

    static Sprite LoadSprite(string name)
    {
        string path = PlaceholderArt.Dir + "/" + name + ".png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogError("[Flappy] Sprite não encontrado: " + path + " — rode 'Flappy/1 - Gerar arte placeholder' antes.");
        return sprite;
    }

    /// <summary>
    /// As peças vetorizadas em que o avião se desfaz. A lista sai da pasta, não
    /// do código: quem exportar mais uma peça da prancheta vê-a nos destroços
    /// sem mexer aqui. Se vierem nulas é porque ainda são texturas em vez de
    /// sprites — é o <c>Flappy/1c</c> que trata disso.
    /// </summary>
    static Sprite[] LoadPlaneParts()
    {
        var paths = PlaceholderArt.PlanePartPaths();

        if (paths.Length == 0)
        {
            Debug.LogError("[Flappy] Nenhuma peça '*" + PlaceholderArt.PartSuffix + ".png' em " +
                           PlaceholderArt.Dir + " — o avião não se vai desfazer em nada.");
            return new Sprite[0];
        }

        var sprites = new Sprite[paths.Length];
        int missing = 0;

        for (int i = 0; i < paths.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
            if (sprites[i] == null) missing++;
        }

        if (missing > 0)
        {
            Debug.LogError("[Flappy] " + missing + " de " + paths.Length + " peças do avião não são Sprite — " +
                           "rode 'Flappy/1c - Ajustar importação da arte manual' e monte a cena outra vez.");
        }

        return sprites;
    }

    /// <summary>
    /// Tira o chapéu do monte dos destroços e devolve-o à parte. É a única
    /// peça reconhecida pelo nome — o resto do projeto evita isso, mas aqui
    /// não há alternativa: nada no desenho diz que aquela peça é um chapéu.
    /// Se alguém a renomear, perde-se o gag e mais nada: os destroços ficam
    /// exatamente como estavam, com o chapéu de volta ao meio deles.
    /// </summary>
    static Sprite TakeHat(List<Sprite> parts)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] == null) continue;
            if (!parts[i].name.StartsWith(HatPart, System.StringComparison.OrdinalIgnoreCase)) continue;

            var hat = parts[i];
            parts.RemoveAt(i);
            return hat;
        }

        Debug.LogWarning("[Flappy] Nenhuma peça '" + HatPart + "*' em " + PlaceholderArt.Dir +
                         " — o avião fica sem o chapéu a cair no fim da queda.");
        return null;
    }

    /// <summary>
    /// Os prédios que fazem de obstáculo, na ordem em que entram no jogo. Como
    /// a lista sai da pasta, exportar um quarto prédio da prancheta chega para
    /// ele entrar na roda. Sem nenhum, fica a torre gerada por código.
    /// </summary>
    public static Sprite[] LoadTowerSprites()
    {
        var paths = PlaceholderArt.TowerPartPaths();

        if (paths.Length == 0)
        {
            Debug.LogWarning("[Flappy] Nenhum prédio '" + PlaceholderArt.TowerPrefix + "*" +
                             PlaceholderArt.PartSuffix + ".png' em " + PlaceholderArt.Dir +
                             " — os obstáculos ficam com a torre gerada por código.");

            var placeholder = LoadSprite("tower");
            return placeholder != null ? new[] { placeholder } : new Sprite[0];
        }

        var sprites = new Sprite[paths.Length];
        int missing = 0;

        for (int i = 0; i < paths.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(paths[i]);
            if (sprites[i] == null) missing++;
        }

        if (missing > 0)
        {
            Debug.LogError("[Flappy] " + missing + " de " + paths.Length + " prédios não são Sprite — " +
                           "rode 'Flappy/1c - Ajustar importação da arte manual' e monte a cena outra vez.");
        }

        return sprites;
    }

    /// <summary>
    /// Emparelha cada prédio com a sua pasta de cacos. Prédio sem pasta — ou com
    /// a pasta vazia — fica de fora da lista e não desaba, que é como se
    /// escolhe quais é que se desfazem: cortando cacos, não mexendo em código.
    /// </summary>
    static void SetTowerDebris(CrashEffects fx, Sprite[] towers)
    {
        var owners = new List<Sprite>();
        var pieces = new List<Sprite[]>();

        foreach (var tower in towers)
        {
            if (tower == null) continue;

            var debris = LoadDebris(tower.name);
            if (debris.Length == 0) continue;

            owners.Add(tower);
            pieces.Add(debris);
        }

        var so = new SerializedObject(fx);
        var array = so.FindProperty("towerDebris");
        array.arraySize = owners.Count;

        for (int i = 0; i < owners.Count; i++)
        {
            var entry = array.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("tower").objectReferenceValue = owners[i];

            var slot = entry.FindPropertyRelative("pieces");
            slot.arraySize = pieces[i].Length;

            for (int j = 0; j < pieces[i].Length; j++)
            {
                slot.GetArrayElementAtIndex(j).objectReferenceValue = pieces[i][j];
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        if (owners.Count == 0)
        {
            Debug.LogWarning("[Flappy] Nenhum prédio tem cacos cortados — nenhum vai desabar. " +
                             "As pastas são 'Assets/Art/Predio<n>Destrocos/1x'.");
            return;
        }

        Debug.Log("[Flappy] " + owners.Count + " de " + towers.Length + " prédios desabam.");
    }

    /// <summary>
    /// Os cacos em que a fachada de um prédio se parte. Como a lista sai da
    /// pasta, cortar mais um caco no Illustrator e exportá-lo para lá chega
    /// para ele entrar no desabamento.
    /// </summary>
    static Sprite[] LoadDebris(string towerName)
    {
        var paths = PlaceholderArt.DebrisPaths(towerName);
        if (paths.Length == 0) return new Sprite[0];

        var sprites = new List<Sprite>(paths.Length);
        int missing = 0;

        foreach (var path in paths)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null) sprites.Add(sprite);
            else missing++;
        }

        if (missing > 0)
        {
            Debug.LogError("[Flappy] " + missing + " de " + paths.Length + " cacos de '" + towerName +
                           "' não são Sprite — rode 'Flappy/1c - Ajustar importação da arte manual'.");
        }

        return sprites.ToArray();
    }

    static AudioClip LoadClip(string name)
    {
        string path = PlaceholderAudio.Dir + "/" + name + ".wav";
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogError("[Flappy] Som não encontrado: " + path + " — rode 'Flappy/1b - Gerar sons placeholder' antes.");
        return clip;
    }

    /// <summary>
    /// Como o LoadClip, mas para som que vem de fora: não é o gerador de
    /// placeholders que o cria, portanto faltar é aviso e não erro.
    /// </summary>
    static AudioClip LoadOptionalClip(string name)
    {
        string path = PlaceholderAudio.Dir + "/" + name + ".wav";
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogWarning("[Flappy] Som de fundo não encontrado: " + path + " — a cena fica sem chuva.");
        return clip;
    }

    static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetRefArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var array = so.FindProperty(field);

        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetVector2(Object target, string field, Vector2 value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
