using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] GameObject obstaclePairPrefab;

    /// <summary>
    /// Os prédios, na ordem em que entram: predio 1, predio 2, predio 3, e
    /// outra vez do início. A lista é montada pelo SceneBuilder a partir da
    /// pasta da arte — exportar um quarto prédio chega para ele entrar na roda.
    /// </summary>
    [SerializeField] Sprite[] towerSprites;

    [Tooltip("O prédio que traz estendal no telhado — o prédio 4. Vazio: não há " +
             "estendais. O SceneBuilder liga-o sozinho.")]
    [SerializeField] Sprite clotheslineTower;

    [Tooltip("Quem decide se este par traz pombo. Vazio: não há pombos.")]
    [SerializeField] PigeonSpawner pigeons;

    [Tooltip("Quem decide se este par traz moedas. Vazio: não há moedas.")]
    [SerializeField] CoinSpawner coins;

    GameManager game;
    float timer;
    int nextTower;

    void Start()
    {
        game = GameManager.Instance;
    }

    void Update()
    {
        if (game == null || game.State != GameState.Playing) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        // por distância, não por tempo: assim o espaçamento entre pares
        // continua igual à medida que o jogo acelera
        timer = config.spawnSpacing / Mathf.Max(0.5f, game.CurrentSpeed);
        Spawn();
    }

    void Spawn()
    {
        var go = Instantiate(obstaclePairPrefab, new Vector3(config.spawnX, 0f, 0f), Quaternion.identity, transform);
        go.SetActive(true);
        var tower = NextTower();
        float gapCenter = Random.Range(config.gapCenterMin, config.gapCenterMax);
        bool clothesline = RollClothesline(tower);

        go.GetComponent<ObstaclePair>().Init(config, gapCenter, tower, clothesline);

        // duas piadas no mesmo par tiram a graça uma à outra: com estendal, não há pombo
        if (pigeons != null) pigeons.OnPairSpawned(gapCenter, !clothesline);

        // as moedas nascem entre este par e o seguinte, não em cima dele: não
        // disputam espaço com o estendal nem com o pombo, por isso vão à parte
        if (coins != null) coins.OnPairSpawned();
    }

    /// <summary>
    /// Se este obstáculo leva estendal. Primeiro o prédio, só depois a sorte:
    /// o estendal foi desenhado para o telhado do prédio 4 — nos outros ficava
    /// a flutuar por cima de um telhado de duas águas ou espetado numa antena.
    /// </summary>
    bool RollClothesline(Sprite tower)
    {
        if (tower == null || tower != clotheslineTower) return false;

        return Random.value < config.clotheslineChance;
    }

    /// <summary>
    /// O prédio da vez. A altura do vão é sorteada, esta não: os prédios saem
    /// sempre pela mesma ordem, e cada partida recomeça no primeiro porque
    /// reiniciar recarrega a cena.
    /// </summary>
    Sprite NextTower()
    {
        if (towerSprites == null || towerSprites.Length == 0) return null;

        var sprite = towerSprites[nextTower];
        nextTower = (nextTower + 1) % towerSprites.Length;
        return sprite;
    }
}
