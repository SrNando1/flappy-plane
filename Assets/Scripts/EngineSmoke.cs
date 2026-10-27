using UnityEngine;

/// <summary>
/// Rasto de fumo do escape enquanto o avião voa. Com o dedo no ecrã o motor
/// puxa e o fumo sai denso; sem dedo fica só um fio a esmorecer.
///
/// Os sopros são largados no mundo e não são filhos do avião: o avião
/// desaparece na queda e o rasto que ele deixou tem de continuar lá.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EngineSmoke : MonoBehaviour
{
    [SerializeField] Sprite puff;

    [Header("Ritmo — sopros por segundo")]
    [SerializeField] float rateEngineOn = 26f;
    [SerializeField] float rateEngineOff = 9f;

    [Header("Escape")]
    [Tooltip("Sítio de onde sai o fumo, em fração do sprite do avião. " +
             "X negativo é a cauda, por isso o rasto acompanha a arte se ela mudar.")]
    [SerializeField] Vector2 nozzleFraction = new Vector2(-0.46f, -0.05f);

    [Tooltip("Espalhamento do ponto de saída, para o rasto não ser uma linha perfeita.")]
    [SerializeField] float spread = 0.05f;

    [Header("Sopro")]
    [SerializeField] float lifetime = 0.85f;
    [SerializeField] float startScale = 0.5f;
    [SerializeField] float endScale = 1.9f;

    [Tooltip("Quanto o fumo sobe enquanto se dissipa.")]
    [SerializeField] float rise = 0.5f;

    [Tooltip("Empurrão para trás somado à velocidade do cenário, para os sopros " +
             "saírem do escape em vez de nascerem colados a ele.")]
    [SerializeField] float exhaustPush = 0.7f;

    [Tooltip("Opacidade do sopro com o motor ligado e com ele desligado.")]
    [SerializeField] float alphaEngineOn = 0.5f;
    [SerializeField] float alphaEngineOff = 0.24f;

    [Tooltip("Fica atrás do avião (20) e à frente das torres (5).")]
    [SerializeField] int sortingOrder = 19;

    GameManager game;
    Airplane plane;
    Transform trail;
    Vector2 nozzle;
    float pending;

    void Awake()
    {
        SetNozzle(nozzleFraction);
        plane = GetComponent<Airplane>();
    }

    /// <summary>Muda o escape, em fração do sprite — para quando o avião troca de desenho.</summary>
    public void SetNozzle(Vector2 fraction)
    {
        nozzleFraction = fraction;

        var sprite = GetComponent<SpriteRenderer>().sprite;
        var size = sprite != null ? (Vector2)sprite.bounds.size : Vector2.one;
        nozzle = nozzleFraction * size;
    }

    void Start()
    {
        game = GameManager.Instance;
        trail = new GameObject("EngineSmoke").transform;
    }

    void Update()
    {
        if (puff == null || game == null || game.State == GameState.GameOver) return;

        // pergunta ao avião e não ao dedo: com o motor engasgado pelo pombo, o
        // dedo está no ecrã mas não sai fumo de quem puxa
        bool engineOn = plane != null
            ? plane.EngineRunning
            : game.State == GameState.Playing && GameInput.Held;

        pending += (engineOn ? rateEngineOn : rateEngineOff) * Time.deltaTime;
        // um engasgo do jogo não pode virar um jorro de fumo de uma vez só
        pending = Mathf.Min(pending, 4f);

        while (pending >= 1f)
        {
            pending -= 1f;
            Emit(engineOn);
        }
    }

    /// <summary>
    /// O motor a tossir: meia dúzia de baforadas escuras de uma vez. É o que
    /// diz ao jogador que a queda não é culpa do dedo dele.
    /// </summary>
    public void Cough(int puffs)
    {
        if (puff == null || game == null) return;

        for (int i = 0; i < puffs; i++)
        {
            var sr = Emit(false);
            var fx = sr.GetComponent<FxParticle>();
            fx.velocity *= Random.Range(1.2f, 1.8f);
            fx.endScale *= 1.4f;
            fx.startColor = new Color(0.22f, 0.22f, 0.25f, 0.75f);
            fx.endColor = new Color(0.4f, 0.4f, 0.44f, 0f);
            sr.color = fx.startColor;
        }
    }

    SpriteRenderer Emit(bool engineOn)
    {
        var at = transform.TransformPoint(nozzle) + (Vector3)(Random.insideUnitCircle * spread);

        var go = new GameObject("Smoke");
        go.transform.SetParent(trail, false);
        go.transform.position = at;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = puff;
        sr.sortingOrder = sortingOrder;

        var fx = go.AddComponent<FxParticle>();

        // sem arrasto: o sopro fica parado no ar e é o cenário que passa por ele
        fx.velocity = new Vector2(
            -(game.CurrentSpeed + exhaustPush * Random.Range(0.7f, 1.3f)),
            Random.Range(-0.15f, 0.15f));
        fx.gravity = -rise;   // negativo faz subir, como o fumo
        fx.angularVelocity = Random.Range(-45f, 45f);
        fx.lifetime = lifetime * Random.Range(0.85f, 1.15f);
        fx.startScale = startScale * Random.Range(0.8f, 1.2f);
        fx.endScale = endScale * Random.Range(0.85f, 1.2f);

        float alpha = engineOn ? alphaEngineOn : alphaEngineOff;
        fx.startColor = new Color(0.8f, 0.8f, 0.85f, alpha * Random.Range(0.8f, 1.1f));
        fx.endColor = new Color(0.95f, 0.95f, 1f, 0f);

        // o FxParticle só aplica cor e escala no Start: sem isto o primeiro
        // frame de cada sopro era um clarão branco em tamanho real
        sr.color = fx.startColor;
        go.transform.localScale = Vector3.one * fx.startScale;
        return sr;
    }
}
