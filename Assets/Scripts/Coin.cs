using UnityEngine;

/// <summary>
/// Uma moeda parada no céu entre dois obstáculos, a girar sobre o próprio eixo e a
/// flutuar de leve. O avião atravessa-a e ela some numa faísca dourada — não
/// mata nem pontua, só enche a carteira.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Coin : MonoBehaviour
{
    /// <summary>Voltas por segundo do giro em torno do eixo vertical, simulado a esticar a escala X.</summary>
    const float SpinRate = 1.1f;

    const float BobHeight = 0.08f;
    const float BobRate = 1.6f;

    const int SparkleCount = 8;

    Sprite spark;
    GameManager game;
    SpriteRenderer sr;

    float baseY;
    float age;
    bool collected;

    /// <param name="sparkSprite">A faísca que se espalha ao apanhar a moeda. Vazio: some sem festa.</param>
    public void Init(Sprite sparkSprite)
    {
        spark = sparkSprite;
        baseY = transform.position.y;
        // cada moeda começa o giro e o balanço num ponto diferente do ciclo
        age = Random.value;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        game = GameManager.Instance;
    }

    void Update()
    {
        if (game == null) return;

        float dt = Time.deltaTime;
        if (game.State == GameState.Playing) age += dt;

        // um giro de moeda sem segundo fotograma: a escala X passa por zero e
        // volta, como a moeda virada de perfil no meio da volta
        var scale = transform.localScale;
        scale.x = Mathf.Cos(age * SpinRate * Mathf.PI * 2f);
        transform.localScale = scale;

        var p = transform.position;
        p.x -= Scroll() * dt;
        p.y = baseY + Mathf.Sin(age * BobRate * Mathf.PI * 2f) * BobHeight;
        transform.position = p;

        if (transform.position.x < game.Config.despawnX) Destroy(gameObject);
    }

    /// <summary>O cenário a passar. Parado fora da partida, como o resto do mundo.</summary>
    float Scroll()
    {
        return game.State == GameState.Playing ? game.CurrentSpeed : 0f;
    }

    /// <summary>A apanha. Verdadeiro só da primeira vez — um trigger não pode contar em dobro.</summary>
    public bool Collect()
    {
        if (collected) return false;
        collected = true;

        Sparkle();
        Destroy(gameObject);
        return true;
    }

    void Sparkle()
    {
        if (spark == null) return;

        for (int i = 0; i < SparkleCount; i++)
        {
            var go = new GameObject("CoinSparkle");
            go.transform.position = transform.position;

            var particleSr = go.AddComponent<SpriteRenderer>();
            particleSr.sprite = spark;
            particleSr.sortingOrder = sr.sortingOrder + 1;

            var p = go.AddComponent<FxParticle>();
            p.velocity = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f);
            p.drag = 4f;
            p.lifetime = Random.Range(0.25f, 0.4f);
            p.startScale = Random.Range(0.12f, 0.2f);
            p.endScale = 0f;
            p.startColor = new Color(1f, 0.87f, 0.35f, 1f);
            p.endColor = new Color(1f, 0.87f, 0.35f, 0f);
            p.fadeStart = 0.3f;

            // o FxParticle só aplica escala e cor no Start (ver EngineSmoke.Emit)
            particleSr.color = p.startColor;
            go.transform.localScale = Vector3.one * p.startScale;
        }
    }
}
