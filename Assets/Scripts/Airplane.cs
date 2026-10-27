using System.Collections;
using UnityEngine;

/// <summary>
/// Com o dedo no ecrã o motor está ligado: o avião persegue a altura do dedo.
/// Sem dedo o motor desliga e a gravidade assume.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Airplane : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] CrashEffects crash;

    [Tooltip("Os olhos de desenho animado. Ficam desligados a partida toda e " +
             "só acendem durante a pausa do embate.")]
    [SerializeField] SpriteRenderer eyes;

    /// <summary>
    /// A que ritmo o mundo anda durante a pausa. Não é zero de propósito: a
    /// zero o avião congelava a meio de um fotograma e lia-se como o jogo a
    /// travar. A dois por cento ainda se vê tudo a arrastar-se.
    /// </summary>
    const float HitstopScale = 0.02f;

    Rigidbody2D rb;
    PlaneLaundry laundry;
    EngineSmoke smoke;
    Camera cam;
    GameManager game;
    bool dead;
    float idleBaseY;

    /// <summary>Até quando o motor fica engasgado pelo pombo, em Time.time.</summary>
    float stalledUntil = -1f;

    bool Stalled => Time.time < stalledUntil;

    /// <summary>
    /// Se o motor está mesmo a puxar: partida a correr, dedo no ecrã e nenhum
    /// pombo entalado na hélice. É o que o fumo e o som devem seguir, não o dedo.
    /// </summary>
    public bool EngineRunning =>
        !dead && game != null && game.State == GameState.Playing && GameInput.Held && !Stalled;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        laundry = GetComponent<PlaneLaundry>();
        smoke = GetComponent<EngineSmoke>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        idleBaseY = transform.position.y;
    }

    void Start()
    {
        game = GameManager.Instance;
        cam = Camera.main;

        Wear(game.SelectedPlane);
        game.PlaneChanged += Wear;
    }

    void OnDestroy()
    {
        if (game != null) game.PlaneChanged -= Wear;
    }

    /// <summary>
    /// Veste o avião da loja: troca o desenho e acerta o colisor, os olhos, o
    /// alfinete da roupa e o escape ao tamanho dele. Com a partida parada, na
    /// tela inicial, vê-se a troca logo ali.
    /// </summary>
    void Wear(PlaneSkin skin)
    {
        if (skin == null || skin.sprite == null) return;

        var sr = GetComponent<SpriteRenderer>();
        sr.sprite = skin.sprite;
        Vector2 size = skin.sprite.bounds.size;

        var col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            col.size = Vector2.Scale(skin.colliderSize, size);
            col.offset = Vector2.Scale(skin.colliderOffset, size);
        }

        if (eyes != null) eyes.transform.localPosition = Vector2.Scale(skin.cockpit, size);
        if (laundry != null) laundry.SetPin(Vector2.Scale(skin.nose, size));
        if (smoke != null) smoke.SetNozzle(skin.nozzle);
    }

    void Update()
    {
        if (game == null) return;

        if (game.State == GameState.Ready)
        {
            Idle();
            // no ecrã é o botão START que arranca (ver StartButton); a barra
            // de espaço fica como atalho para testar no Editor
            if (Input.GetKeyDown(KeyCode.Space)) game.StartGame();
        }

        Tilt();
    }

    void FixedUpdate()
    {
        if (game == null || dead || game.State != GameState.Playing) return;

        bool running = EngineRunning;
        if (AudioManager.Instance != null) AudioManager.Instance.SetEngine(running);

        if (running) EngineOn();
        // dedo levantado cai como sempre, com pombo ou sem ele
        else if (Stalled && GameInput.Held) Sputter();
        else EngineOff();

        ClampCeiling();
    }

    /// <summary>Flutuação suave enquanto espera o primeiro toque.</summary>
    void Idle()
    {
        var p = transform.position;
        p.y = idleBaseY + Mathf.Sin(Time.time * 2.5f) * 0.1f;
        transform.position = p;
    }

    /// <summary>
    /// Persegue a altura do dedo com velocidade limitada — o avião não teleporta
    /// para debaixo do dedo, ele voa até lá.
    /// </summary>
    void EngineOn()
    {
        rb.gravityScale = 0f;

        float fingerY = FingerWorldY();
        float desired = Mathf.Clamp(
            (fingerY - rb.position.y) * config.followResponsiveness,
            -config.followSpeed,
            config.followSpeed);

        float vy = Mathf.MoveTowards(rb.linearVelocity.y, desired, config.followAccel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(0f, vy);
    }

    void EngineOff()
    {
        rb.gravityScale = config.gravityScale;

        if (rb.linearVelocity.y < -config.maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(0f, -config.maxFallSpeed);
        }
    }

    /// <summary>
    /// O engasgo: o motor não responde ao dedo, mas o avião também não cai a
    /// pique como com o dedo levantado. Afunda o bastante para assustar e
    /// pouco o bastante para dar para recuperar — o pombo não mata.
    /// </summary>
    void Sputter()
    {
        rb.gravityScale = config.pigeonStallGravity;

        if (rb.linearVelocity.y < -config.maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(0f, -config.maxFallSpeed);
        }
    }

    /// <summary>Levou com um pombo: o motor tosse, a câmara estremece e o jogo fica a saber.</summary>
    void HitPigeon()
    {
        stalledUntil = Time.time + config.pigeonStallDuration;

        // a subir, o avião perde o embalo de uma vez; a descer, mantém a
        // queda que já trazia — o engasgo não pode virar um travão no ar
        if (rb.linearVelocity.y > 0f) rb.linearVelocity = Vector2.zero;

        if (smoke != null) smoke.Cough(6);

        var shake = cam != null ? cam.GetComponent<CameraShake>() : null;
        if (shake != null) shake.Shake(0.12f, 0.3f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayPigeon();
        GameManager.Instance.PigeonStrike();
    }

    float FingerWorldY()
    {
        var screen = new Vector3(GameInput.ScreenPosition.x, GameInput.ScreenPosition.y, 10f);
        return cam.ScreenToWorldPoint(screen).y;
    }

    void ClampCeiling()
    {
        if (rb.position.y <= config.ceilingY) return;

        rb.position = new Vector2(rb.position.x, config.ceilingY);
        if (rb.linearVelocity.y > 0f) rb.linearVelocity = Vector2.zero;
    }

    /// <summary>Levanta o nariz ao subir e mergulha ao cair.</summary>
    void Tilt()
    {
        float target = game != null && game.State == GameState.Ready
            ? 0f
            : Mathf.Lerp(config.downTilt, config.upTilt, Mathf.InverseLerp(-6f, 4f, rb.linearVelocity.y));

        float z = Mathf.LerpAngle(transform.eulerAngles.z, target, Time.deltaTime * config.tiltSpeed);
        transform.rotation = Quaternion.Euler(0f, 0f, z);
    }

    void Die(bool hitLightning = false, bool keepScreenClear = false)
    {
        if (dead) return;
        dead = true;

        // o motor cala-se já: é o silêncio que faz a pausa ouvir-se
        if (AudioManager.Instance != null) AudioManager.Instance.SetEngine(false);

        StartCoroutine(CrashSequence(hitLightning, keepScreenClear));
    }

    /// <summary>
    /// A lei mais velha do desenho animado: primeiro o coiote percebe, só
    /// depois é que cai. O mundo pára quase por completo, o avião olha para
    /// quem está a jogar, e só quando a pausa acaba é que a desgraça começa —
    /// o fim de jogo, o som do embate e os destroços vêm todos depois.
    /// </summary>
    IEnumerator CrashSequence(bool hitLightning, bool keepScreenClear)
    {
        if (eyes != null) eyes.enabled = true;

        Time.timeScale = HitstopScale;
        // tempo real: com o timeScale no chão, um WaitForSeconds normal
        // esperaria cinquenta vezes mais do que o número diz
        yield return new WaitForSecondsRealtime(config.hitstopDuration);
        Time.timeScale = 1f;

        if (eyes != null) eyes.enabled = false;

        GameManager.Instance.GameOver(hitLightning);

        // a animação de queda assume daqui: o avião real sai de cena
        if (crash != null)
        {
            crash.Play(transform.position, GetComponent<SpriteRenderer>().sprite, keepScreenClear);
        }

        // a roupa que ia na hélice solta-se com o resto, em vez de sumir
        if (laundry != null) laundry.LetGo(crash);

        // desligar o objeto mata esta corrotina — é por isso que é a última linha
        gameObject.SetActive(false);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (dead) return;

        var hit = collision.collider;

        // o relâmpago tem estalo próprio; o resto — prédio, chão — leva o crash.
        // Pelo nome, como o ObstaclePair já faz para lhe chegar.
        if (hit.name == "Tower")
        {
            var impact = collision.contactCount > 0
                ? collision.GetContact(0).point
                : (Vector2)transform.position;

            Die(false, Collapse(hit, impact));
            return;
        }

        Die(hit.name == "Lightning");
    }

    /// <summary>
    /// Manda a fachada abaixo e apaga o prédio, se for dos que se desfazem.
    /// Esconder o desenho é metade do efeito: sem isso viam-se os cacos a voar
    /// por cima de uma parede ainda inteira.
    /// </summary>
    /// <returns>Verdadeiro se o prédio desabou mesmo.</returns>
    bool Collapse(Collider2D tower, Vector2 impact)
    {
        if (crash == null) return false;

        var pair = tower.GetComponentInParent<ObstaclePair>();
        if (pair == null) return false;

        // primeiro pergunta, só depois apaga: um prédio sem cacos cortados
        // desapareceria da tela sem largar nada
        if (!crash.PlayTowerCollapse(impact, tower.bounds, pair.TowerSprite)) return false;

        var sr = tower.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        pair.KnockOffClothesline(crash);
        return true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        if (other.GetComponent<ScoreZone>() != null)
        {
            GameManager.Instance.AddScore();
            return;
        }

        // o pombo também não mata: engasga o motor e vai-se embora ofendido
        var pigeon = other.GetComponent<Pigeon>();
        if (pigeon != null)
        {
            if (pigeon.Strike()) HitPigeon();
            return;
        }

        var coin = other.GetComponent<Coin>();
        if (coin != null)
        {
            if (coin.Collect()) GameManager.Instance.CollectCoin();
            return;
        }

        // o estendal não mata nem dá pontos: só veste a hélice
        var line = other.GetComponent<Clothesline>();
        if (line != null && line.Snag() && laundry != null) laundry.Wear();
    }
}
