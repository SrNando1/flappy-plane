using UnityEngine;

/// <summary>
/// Um sítio só para todo o som. Os efeitos curtos saem por PlayOneShot;
/// o motor é uma fonte em ciclo cujo volume e altura variam com o jogo;
/// a chuva de fundo é uma terceira fonte, que atravessa os reinícios.
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>Onde fica guardada a escolha do jogador quanto ao som de fundo.</summary>
    public const string MusicKey = "flappy.music";

    public static AudioManager Instance { get; private set; }

    [SerializeField] GameConfig config;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource engineSource;
    [SerializeField] AudioSource musicSource;

    [SerializeField] AudioClip score;
    [SerializeField] AudioClip crash;
    [SerializeField] AudioClip start;
    [SerializeField] AudioClip record;
    [Tooltip("Som próprio de bater no relâmpago. Vazio: toca o crash normal.")]
    [SerializeField] AudioClip lightning;
    [Tooltip("Pancada no pombo: baque, grasnido e o motor a tossir.")]
    [SerializeField] AudioClip pigeon;

    [Tooltip("Apanhar uma moeda.")]
    [SerializeField] AudioClip coin;

    [Header("Motor")]
    [SerializeField] float engineVolume = 0.45f;
    [Tooltip("Quantas vezes por segundo o volume do motor percorre a escala toda.")]
    [SerializeField] float engineFade = 6f;
    [SerializeField] float enginePitchMin = 0.92f;
    [SerializeField] float enginePitchMax = 1.35f;

    [Header("Fundo")]
    [Tooltip("Volume da chuva. 0,12 é cerca de -18 dB: ouve-se sem se dar por ela. " +
             "Pressupõe o ficheiro normalizado a -16 LUFS.")]
    [SerializeField] float musicVolume = 0.12f;
    [Tooltip("Quanto do volume sobra enquanto o painel de fim de jogo está no ecrã. " +
             "0,45 é cerca de 7 dB abaixo do normal.")]
    [SerializeField] float musicDuck = 0.45f;
    [Tooltip("Segundos que a chuva leva a entrar, a sair e a baixar no embate.")]
    [SerializeField] float musicFade = 0.8f;

    bool engineOn;

    /// <summary>Se o jogador quer chuva de fundo. Guardado entre sessões.</summary>
    public bool MusicOn { get; private set; }

    void Awake()
    {
        // o reinício recarrega a cena (GameManager.Restart) e sem isto a chuva
        // voltava ao princípio a cada morte. O som passa a viver fora da cena;
        // a cópia que vem com a cena nova despede-se aqui.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (engineSource != null) engineSource.volume = 0f;

        MusicOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        if (musicSource != null) musicSource.volume = 0f;   // entra sempre em fade
    }

    void Update()
    {
        UpdateEngine();
        UpdateMusic();
    }

    /// <summary>
    /// Os fades de som andam em tempo real, não no tempo do jogo. A pausa do
    /// embate põe o <c>timeScale</c> a 2 % e um <c>Time.deltaTime</c> normal
    /// esticaria o desligar do motor de um sexto de segundo para oito — o
    /// avião ficava parado no ar com o motor a roncar, que é o contrário do
    /// que a pausa quer. Fora da pausa os dois relógios são o mesmo.
    /// </summary>
    static float FadeDelta => Time.unscaledDeltaTime;

    void UpdateEngine()
    {
        if (engineSource == null) return;

        float target = engineOn ? engineVolume : 0f;
        engineSource.volume = Mathf.MoveTowards(
            engineSource.volume, target, engineVolume * engineFade * FadeDelta);

        if (engineSource.volume > 0.001f)
        {
            if (!engineSource.isPlaying) engineSource.Play();
        }
        else if (engineSource.isPlaying)
        {
            engineSource.Pause();
        }

        // o motor esforça-se mais à medida que o jogo acelera
        var game = GameManager.Instance;
        if (game != null && config != null)
        {
            float progress = Mathf.InverseLerp(config.obstacleSpeed, config.maxObstacleSpeed, game.CurrentSpeed);
            engineSource.pitch = Mathf.Lerp(enginePitchMin, enginePitchMax, progress);
        }
    }

    void UpdateMusic()
    {
        // sem ficheiro de chuva o jogo corre na mesma, apenas em seco
        if (musicSource == null || musicSource.clip == null) return;

        float target = MusicOn ? musicVolume : 0f;

        // no fim de jogo a chuva sai da frente, para o embate e o painel
        // ficarem com o ecrã só para eles. Volta sozinha no reinício: a cena
        // nova traz um GameManager em Ready.
        var game = GameManager.Instance;
        if (game != null && game.State == GameState.GameOver) target *= musicDuck;

        float step = musicVolume / Mathf.Max(musicFade, 0.01f);
        musicSource.volume = Mathf.MoveTowards(musicSource.volume, target, step * FadeDelta);

        if (musicSource.volume > 0.001f)
        {
            if (!musicSource.isPlaying) musicSource.Play();
        }
        else if (musicSource.isPlaying)
        {
            // Pause e não Stop: quem desliga e volta a ligar retoma onde ia
            musicSource.Pause();
        }
    }

    public void SetEngine(bool on)
    {
        engineOn = on;
    }

    /// <summary>Liga ou desliga a chuva de fundo e guarda a escolha.</summary>
    public void SetMusic(bool on)
    {
        MusicOn = on;
        PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ToggleMusic() => SetMusic(!MusicOn);

    public void PlayScore() => Play(score, 0.6f);
    public void PlayStart() => Play(start, 0.55f);
    public void PlayRecord() => Play(record, 0.7f);
    public void PlayPigeon() => Play(pigeon, 0.8f);
    public void PlayCoin() => Play(coin, 0.55f);

    /// <param name="hitLightning">
    /// Bateu no relâmpago em vez do prédio ou do chão: leva som próprio.
    /// </param>
    public void PlayCrash(bool hitLightning = false)
    {
        // o motor morre no impacto, sem desvanecer
        engineOn = false;
        if (engineSource != null)
        {
            engineSource.volume = 0f;
            engineSource.Stop();
        }

        if (hitLightning && lightning != null) Play(lightning, 0.85f);
        else Play(crash, 0.85f);
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
