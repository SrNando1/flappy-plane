using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Um prédio a subir do chão e um relâmpago a descer do céu,
/// com a zona de pontuação no vão entre os dois.
/// </summary>
public class ObstaclePair : MonoBehaviour
{
    /// <summary>Quanto o prédio entra por baixo do chão, para não abrir fresta.</summary>
    const float GroundOverlap = 0.25f;

    /// <summary>Reaproveitada a cada obstáculo, para não deixar lixo por recolher.</summary>
    static readonly List<Vector2> shapePoints = new List<Vector2>();

    GameConfig config;
    GameManager game;

    /// <summary>
    /// O prédio que está mesmo na tela. Sai do SpriteRenderer e não do
    /// parâmetro do <see cref="Init"/>: quando esse vem nulo fica o do prefab,
    /// e é nesse que o avião vai bater.
    /// </summary>
    public Sprite TowerSprite { get; private set; }

    /// <param name="towerSprite">
    /// O prédio que calha a este obstáculo. Nulo deixa ficar o do prefab.
    /// </param>
    /// <param name="withClothesline">
    /// Se leva estendal no telhado. Quem decide é o spawner, que sabe que
    /// prédios têm telhado plano.
    /// </param>
    public void Init(GameConfig cfg, float gapCenterY, Sprite towerSprite = null, bool withClothesline = false)
    {
        config = cfg;
        game = GameManager.Instance;

        float halfGap = cfg.obstacleGap * 0.5f;

        // o prédio tem pivô no telhado e cresce para baixo
        StretchTower(cfg, gapCenterY - halfGap, towerSprite);
        PlaceClothesline(gapCenterY - halfGap, withClothesline);

        // o raio tem pivô na ponta e cresce para cima
        StretchLightning(cfg, gapCenterY + halfGap);

        var zone = transform.Find("ScoreZone");
        zone.localPosition = new Vector3(0f, gapCenterY, 0f);
        zone.GetComponent<BoxCollider2D>().size = new Vector2(0.2f, cfg.obstacleGap);
    }

    /// <summary>
    /// Pendura o prédio pelo telhado na borda de baixo do vão e estica-o o que
    /// falta para encostar ao chão. Só estica, nunca encolhe: um prédio que já
    /// seja mais alto que o preciso afunda o resto atrás da faixa do chão, que
    /// passa à frente dele. Os prédios são desenhos com andares repetidos, por
    /// isso o esticão lê-se como pé-direito mais alto, não como deformação.
    /// </summary>
    void StretchTower(GameConfig cfg, float roofY, Sprite towerSprite)
    {
        var tower = transform.Find("Tower");
        tower.localPosition = new Vector3(0f, roofY, 0f);

        var sr = tower.GetComponent<SpriteRenderer>();
        if (towerSprite != null) sr.sprite = towerSprite;

        var sprite = sr.sprite;
        TowerSprite = sprite;
        if (sprite == null) return;   // sem arte importada, fica no tamanho do prefab

        float spriteHeight = sprite.bounds.size.y;
        float needed = roofY - (cfg.groundTopY - GroundOverlap);

        tower.localScale = new Vector3(1f, Mathf.Max(1f, needed / spriteHeight), 1f);

        // o colisor segue a silhueta do desenho, como o do relâmpago: com uma
        // caixa, o avião morria no ar vazio ao lado da antena de um prédio e do
        // beiral do telhado do outro. Como é filho da mesma escala, o esticão
        // apanha-o de caminho.
        ApplyPhysicsShape(tower.GetComponent<PolygonCollider2D>(), sprite);
    }

    /// <summary>
    /// Pousa o estendal no telhado, ou esconde-o. É irmão do prédio e não filho:
    /// se fosse filho apanhava o esticão vertical do prédio e a roupa saía
    /// comprida como um lençol.
    /// </summary>
    void PlaceClothesline(float roofY, bool show)
    {
        // sem ele é porque o prefab é de uma versão anterior: rodar
        // 'Flappy/2 - Montar cena do jogo' põe-no lá
        var line = transform.Find("Clothesline");
        if (line == null) return;

        var sr = line.GetComponent<SpriteRenderer>();
        bool on = show && sr != null && sr.sprite != null;

        line.gameObject.SetActive(on);
        if (on) line.localPosition = new Vector3(0f, roofY, 0f);
    }

    /// <summary>
    /// O prédio desabou: o estendal não fica pendurado no ar por cima do
    /// buraco, vai com os cacos.
    /// </summary>
    public void KnockOffClothesline(CrashEffects crash)
    {
        var line = transform.Find("Clothesline");
        if (line == null || !line.gameObject.activeSelf) return;

        var sr = line.GetComponent<SpriteRenderer>();
        if (crash != null && sr != null) crash.ThrowLoose(sr.sprite, sr.bounds.center);

        line.gameObject.SetActive(false);
    }

    /// <summary>
    /// Copia a silhueta do sprite para o colisor. Sem isto o obstáculo ficava
    /// com a forma do prédio que estava guardado no prefab, fosse qual fosse o
    /// prédio que lhe calhou.
    /// </summary>
    static void ApplyPhysicsShape(PolygonCollider2D collider, Sprite sprite)
    {
        // sem colisor em polígono é porque o prefab é de uma versão anterior:
        // rodar 'Flappy/2 - Montar cena do jogo' põe-no lá
        if (collider == null) return;

        int paths = sprite.GetPhysicsShapeCount();
        if (paths == 0) return;   // sprite sem forma física: fica a do prefab

        collider.pathCount = paths;
        for (int i = 0; i < paths; i++)
        {
            sprite.GetPhysicsShape(i, shapePoints);
            collider.SetPath(i, shapePoints);
        }
    }

    /// <summary>
    /// O sprite do relâmpago é curto. Estica-se na vertical para cobrir desde a
    /// borda do vão até fora do ecrã — de caminho, cada relâmpago sai com um
    /// comprimento diferente, que é o que a natureza faz.
    /// </summary>
    void StretchLightning(GameConfig cfg, float tipY)
    {
        var lightning = transform.Find("Lightning");
        lightning.localPosition = new Vector3(0f, tipY, 0f);

        var sprite = lightning.GetComponent<SpriteRenderer>().sprite;
        if (sprite == null) return;   // sem arte importada, fica no tamanho do prefab

        float spriteHeight = sprite.bounds.size.y;
        float needed = cfg.lightningTopY - tipY;

        lightning.localScale = new Vector3(1f, Mathf.Max(0.4f, needed / spriteHeight), 1f);
    }

    void Update()
    {
        if (game == null || game.State != GameState.Playing) return;

        transform.Translate(Vector3.left * (game.CurrentSpeed * Time.deltaTime), Space.World);

        if (transform.position.x < config.despawnX) Destroy(gameObject);
    }
}
