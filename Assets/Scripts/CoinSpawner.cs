using UnityEngine;

/// <summary>
/// Decide quando aparecem moedas e larga-as numa linha horizontal. Não tem
/// relógio próprio: é o <see cref="ObstacleSpawner"/> que avisa a cada par, e a
/// linha nasce no céu aberto a meio caminho entre esse par e o seguinte — longe
/// dos prédios e dos relâmpagos, por isso nenhuma moeda fica encostada a um.
/// </summary>
public class CoinSpawner : MonoBehaviour
{
    [SerializeField] GameConfig config;

    [SerializeField] Sprite coinSprite;

    [Tooltip("A faísca dourada ao apanhar. Vazio: a moeda some sem festa.")]
    [SerializeField] Sprite sparkSprite;

    [Tooltip("Atrás do avião (20) e do pombo (23), à frente do prédio e do relâmpago (5).")]
    [SerializeField] int sortingOrder = 6;

    int pairs;

    public void OnPairSpawned()
    {
        pairs++;

        if (coinSprite == null) return;
        if (pairs <= config.coinFirstPair) return;
        if (Random.value >= config.coinChance) return;

        Spawn();
    }

    void Spawn()
    {
        int min = Mathf.Max(1, config.coinCountMin);
        int count = Random.Range(min, Mathf.Max(min, config.coinCountMax) + 1);

        // a altura sai da mesma faixa dos vãos: o próximo vão ainda não foi
        // sorteado, mas vai cair aqui dentro, por isso a linha fica sempre a
        // caminho de algum lado e nunca obriga a um mergulho sem saída
        float y = Random.Range(config.gapCenterMin, config.gapCenterMax);

        // o par acabou de nascer em spawnX e o seguinte nasce um espaçamento
        // depois — a meio dos dois é onde o céu está mais longe de ambos
        float centerX = config.spawnX + config.spawnSpacing * 0.5f;
        float firstX = centerX - (count - 1) * config.coinSpacing * 0.5f;

        for (int i = 0; i < count; i++)
        {
            SpawnOne(new Vector3(firstX + i * config.coinSpacing, y, 0f));
        }
    }

    void SpawnOne(Vector3 position)
    {
        var go = new GameObject("Coin");
        go.transform.SetParent(transform, false);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite;
        sr.sortingOrder = sortingOrder;

        var body = go.AddComponent<CircleCollider2D>();
        body.isTrigger = true;
        body.radius = Mathf.Max(coinSprite.bounds.extents.x, coinSprite.bounds.extents.y);

        go.AddComponent<Coin>().Init(sparkSprite);
    }
}
