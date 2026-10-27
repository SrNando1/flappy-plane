using UnityEngine;

/// <summary>
/// Um pombo que atravessa o céu contra o avião. Não mata nem dá pontos: quem
/// lhe bate leva com um engasgo do motor, uma nuvem de penas e um letreiro.
/// É perigo falso — o jogador entra em pânico por nada, e é disso que se ri.
///
/// Leva uma vida em três tempos: voa, fica tonto a rodopiar depois da pancada,
/// e foge a bater as asas feito doido, a largar penas pelo caminho.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Pigeon : MonoBehaviour
{
    /// <summary>Batidas de asa por segundo. A fugir bate o dobro.</summary>
    const float FlapRate = 7f;
    const float PanicFlapRate = 16f;

    /// <summary>O sobe-e-desce do voo, a acompanhar as asas. Pequeno: o pombo tem de ser justo.</summary>
    const float BobHeight = 0.05f;

    const float StunDuration = 0.45f;
    const float StunSpin = 1100f;
    const float StunGravity = 10f;

    /// <summary>A fuga: para cima e para trás, contra o vento do avião.</summary>
    static readonly Vector2 FleeVelocity = new Vector2(-2.6f, 2.4f);
    const float FleeFeatherEvery = 0.12f;

    const int BurstFeathers = 12;

    enum Mode { Flying, Stunned, Fleeing }

    Sprite wingsUp;
    Sprite wingsDown;
    Sprite feather;
    float speedFactor;
    float despawnX;

    GameManager game;
    SpriteRenderer sr;
    Collider2D body;
    Transform fx;

    Mode mode = Mode.Flying;
    float baseY;
    float age;
    float modeAge;
    Vector2 velocity;
    float nextFeather;

    /// <param name="speed">
    /// Múltiplo da velocidade do cenário. Sai do <see cref="PigeonSpawner"/>,
    /// que o calcula para o pombo chegar ao avião entre dois prédios.
    /// </param>
    public void Init(Sprite up, Sprite down, Sprite featherSprite, float speed, float despawn)
    {
        wingsUp = up;
        wingsDown = down;
        feather = featherSprite;
        speedFactor = speed;
        despawnX = despawn;

        baseY = transform.position.y;
        // cada pombo começa a bater as asas num ponto diferente do ciclo
        age = Random.value;
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        body = GetComponent<Collider2D>();
    }

    void Start()
    {
        game = GameManager.Instance;
    }

    /// <summary>
    /// A pancada. Verdadeiro só da primeira vez: um pombo tonto a cair em cima
    /// do avião não pode engasgar o motor duas vezes.
    /// </summary>
    public bool Strike()
    {
        if (mode != Mode.Flying) return false;

        mode = Mode.Stunned;
        modeAge = 0f;
        if (body != null) body.enabled = false;

        sr.sprite = wingsUp;
        // atirado para a frente e para cima pelo nariz do avião
        velocity = new Vector2(Random.Range(1.2f, 2f), Random.Range(2.6f, 3.4f));

        FeatherBurst();
        return true;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        modeAge += dt;

        switch (mode)
        {
            case Mode.Flying: Fly(dt); break;
            case Mode.Stunned: Tumble(dt); break;
            default: Flee(dt); break;
        }

        var p = transform.position;
        if (p.x < despawnX || p.y > 7f || p.y < -7f) Destroy(gameObject);
    }

    void Fly(float dt)
    {
        Flap(FlapRate);

        var p = transform.position;
        p.x -= Speed() * dt;
        p.y = baseY + Mathf.Sin(age * FlapRate * Mathf.PI * 2f) * BobHeight;
        transform.position = p;
    }

    void Tumble(float dt)
    {
        velocity.y -= StunGravity * dt;
        transform.position += (Vector3)(velocity * dt) + Vector3.left * (Scroll() * dt);
        transform.Rotate(0f, 0f, StunSpin * dt);

        if (modeAge < StunDuration) return;

        mode = Mode.Fleeing;
        modeAge = 0f;
        velocity = FleeVelocity;
    }

    void Flee(float dt)
    {
        Flap(PanicFlapRate);

        // endireita-se depressa, mas não de uma vez: ainda vai meio zonzo
        float z = Mathf.LerpAngle(transform.eulerAngles.z, 0f, dt * 10f);
        transform.rotation = Quaternion.Euler(0f, 0f, z);

        transform.position += (Vector3)(velocity * dt) + Vector3.left * (Scroll() * dt);

        if (modeAge < nextFeather) return;
        nextFeather = modeAge + FleeFeatherEvery;
        Feather(transform.position, Random.insideUnitCircle * 0.4f, 1.6f);
    }

    void Flap(float rate)
    {
        sr.sprite = Mathf.Repeat(age * rate, 1f) < 0.5f ? wingsUp : wingsDown;
    }

    /// <summary>
    /// A velocidade do pombo no mundo. Com a partida acabada o cenário pára, mas
    /// o pombo não: passa por cima dos destroços como se nada fosse.
    /// </summary>
    float Speed()
    {
        if (game == null) return 0f;

        return game.State == GameState.Playing
            ? game.CurrentSpeed * speedFactor
            : game.CurrentSpeed * (speedFactor - 1f);
    }

    /// <summary>O cenário a passar, para o pombo tonto não ficar colado ao ecrã.</summary>
    float Scroll()
    {
        return game != null && game.State == GameState.Playing ? game.CurrentSpeed : 0f;
    }

    void FeatherBurst()
    {
        for (int i = 0; i < BurstFeathers; i++)
        {
            Feather(transform.position + (Vector3)(Random.insideUnitCircle * 0.12f),
                    Random.insideUnitCircle.normalized * Random.Range(1.2f, 3.4f),
                    Random.Range(1.3f, 2f));
        }
    }

    void Feather(Vector3 at, Vector2 push, float lifetime)
    {
        if (feather == null) return;

        // as penas vivem fora do pombo: ele sai do ecrã e elas ainda estão a cair
        if (fx == null) fx = new GameObject("PigeonFeathers").transform;

        var go = new GameObject("Feather");
        go.transform.SetParent(fx, false);
        go.transform.position = at;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        var feat = go.AddComponent<SpriteRenderer>();
        feat.sprite = feather;
        feat.sortingOrder = sr.sortingOrder + 1;

        var p = go.AddComponent<FxParticle>();
        p.velocity = push;
        // o ar trava o empurrão depressa e a pena fica a descer devagar,
        // levada para trás pelo cenário
        p.drag = 3.2f;
        p.gravity = 0.9f;
        p.drift = Vector2.left * Scroll();
        p.angularVelocity = Random.Range(-320f, 320f);
        p.lifetime = lifetime;
        p.startScale = Random.Range(0.8f, 1.15f);
        p.endScale = p.startScale;
        p.fadeStart = 0.6f;

        float shade = Random.Range(0.82f, 1f);
        p.startColor = new Color(shade, shade, shade * 1.02f, 1f);
        p.endColor = new Color(shade, shade, shade, 0f);

        // o FxParticle só aplica escala e cor no Start (ver EngineSmoke.Emit)
        feat.color = p.startColor;
        go.transform.localScale = Vector3.one * p.startScale;
    }
}
