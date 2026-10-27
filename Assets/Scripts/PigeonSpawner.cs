using UnityEngine;

/// <summary>
/// Decide quando aparece um pombo e larga-o no céu. Não tem relógio próprio: é
/// o <see cref="ObstacleSpawner"/> que avisa a cada par, e o pombo nasce com
/// ele, à altura do vão. Assim cruza-se com o avião mesmo na linha em que o
/// jogador quer voar — mas a meio caminho entre dois prédios, onde sobra
/// espaço para recuperar do engasgo.
/// </summary>
public class PigeonSpawner : MonoBehaviour
{
    [SerializeField] GameConfig config;

    [Tooltip("O avião. Só se lê o X, para acertar o encontro com o pombo.")]
    [SerializeField] Transform plane;

    [SerializeField] Sprite wingsUp;
    [SerializeField] Sprite wingsDown;
    [SerializeField] Sprite feather;

    [Tooltip("À frente do avião (20), dos olhos (21) e da roupa na hélice (22).")]
    [SerializeField] int sortingOrder = 23;

    /// <summary>
    /// O raio do corpo, sem as asas. Um pombo que só se apanha em cheio lê-se
    /// como batota; um que se apanha com a ponta da asa também.
    /// </summary>
    [SerializeField] float bodyRadius = 0.12f;

    int pairs;
    int lastPigeonPair = int.MinValue / 2;

    /// <param name="gapCenterY">A altura do vão do par que acabou de nascer.</param>
    /// <param name="allowed">Falso quando o par já traz outra piada consigo.</param>
    public void OnPairSpawned(float gapCenterY, bool allowed)
    {
        pairs++;

        if (!allowed || wingsUp == null || plane == null) return;
        if (pairs <= config.pigeonFirstPair) return;
        if (pairs - lastPigeonPair <= config.pigeonCooldownPairs) return;
        if (Random.value >= config.pigeonChance) return;

        lastPigeonPair = pairs;
        Spawn(gapCenterY);
    }

    void Spawn(float y)
    {
        var go = new GameObject("Pigeon");
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(config.spawnX, y, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = wingsUp;
        sr.sortingOrder = sortingOrder;

        var body = go.AddComponent<CircleCollider2D>();
        body.isTrigger = true;
        body.radius = bodyRadius;

        go.AddComponent<Pigeon>().Init(wingsUp, wingsDown, feather, SpeedFactor(), config.despawnX);
    }

    /// <summary>
    /// Quantas vezes mais depressa que o cenário o pombo tem de voar para chegar
    /// ao avião quando o par dele ainda vai <c>pigeonLead</c> de espaçamento à
    /// frente. Os dois partem do mesmo X e andam proporcionais à mesma
    /// velocidade, por isso a conta não depende de quão acelerado vai o jogo.
    /// </summary>
    float SpeedFactor()
    {
        float distance = config.spawnX - plane.position.x;
        float lead = config.spawnSpacing * config.pigeonLead;

        // um espaçamento maior que o ecrã deixava a conta negativa: o par
        // nunca chegaria a ir à frente. Aí o pombo voa só o dobro e pronto.
        if (distance - lead < 0.5f) return 2f;

        return distance / (distance - lead);
    }
}
