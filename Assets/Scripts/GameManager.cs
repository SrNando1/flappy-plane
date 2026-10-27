using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public const string HighScoreKey = "flappy.highscore";
    public const string TotalCoinsKey = "flappy.coins";
    public const string SelectedPlaneKey = "flappy.plane";
    public const string OwnedPlaneKeyPrefix = "flappy.plane.owned.";

    public static GameManager Instance { get; private set; }

    [SerializeField] GameConfig config;

    [Tooltip("Os aviões da loja. O primeiro é o de origem.")]
    [SerializeField] PlaneCatalog planes;

    public GameConfig Config => config;
    public PlaneCatalog Planes => planes;

    /// <summary>O avião com que se joga. Nulo só se o catálogo vier vazio.</summary>
    public PlaneSkin SelectedPlane { get; private set; }
    public GameState State { get; private set; } = GameState.Ready;
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public bool NewRecord { get; private set; }

    /// <summary>Moedas apanhadas nesta partida.</summary>
    public int Coins { get; private set; }

    /// <summary>
    /// Moedas apanhadas ao todo, em todas as partidas. Só grava em disco no fim
    /// da partida, como o recorde — gravar a cada moeda seria escrita a mais.
    /// </summary>
    public int TotalCoins { get; private set; }

    /// <summary>
    /// Se a partida acabou num relâmpago em vez de um prédio ou do chão.
    /// Quem morre no meio do céu merece ouvir isso no painel do fim.
    /// </summary>
    public bool HitLightning { get; private set; }

    /// <summary>
    /// Se a partida acabou pouco depois de levar com um pombo. Ele não mata,
    /// mas quem morre logo a seguir vai querer culpá-lo — e o painel deixa.
    /// </summary>
    public bool BlamePigeon { get; private set; }

    /// <summary>
    /// Velocidade de rolagem atual. Sobe com a pontuação e é a fonte única
    /// para os obstáculos, o chão e o intervalo de spawn.
    /// </summary>
    public float CurrentSpeed { get; private set; }

    float TargetSpeed => Mathf.Min(config.obstacleSpeed + Score * config.speedPerPoint, config.maxObstacleSpeed);

    public event Action<GameState> StateChanged;
    public event Action<int> ScoreChanged;
    public event Action<int> CoinsChanged;
    public event Action PigeonStruck;

    /// <summary>A carteira mudou fora de uma partida — uma compra na loja.</summary>
    public event Action<int> TotalCoinsChanged;

    public event Action<PlaneSkin> PlaneChanged;

    float gameOverTime;
    float lastPigeonTime = float.NegativeInfinity;

    void Awake()
    {
        Instance = this;
        Application.targetFrameRate = 60;

        // rede de segurança: a pausa do embate mexe no timeScale e o reinício
        // recarrega a cena. Se alguma coisa correr mal a meio da pausa, a
        // partida seguinte nascia em câmara lenta e ninguém perceberia porquê.
        Time.timeScale = 1f;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        TotalCoins = PlayerPrefs.GetInt(TotalCoinsKey, 0);
        CurrentSpeed = config.obstacleSpeed;

        if (planes != null)
        {
            // um avião gravado que saiu do catálogo, ou que nunca foi pago,
            // volta ao de origem em vez de deixar o jogo sem avião
            var saved = planes.Find(PlayerPrefs.GetString(SelectedPlaneKey, ""));
            SelectedPlane = saved != null && Owns(saved) ? saved : planes.Default;
        }
    }

    void Update()
    {
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, TargetSpeed, config.speedRamp * Time.deltaTime);

        if (State != GameState.GameOver) return;
        // não deixa um toque apressado saltar a animação de queda
        if (Time.unscaledTime - gameOverTime < config.crashDuration + 0.4f) return;
        if (GameInput.TapDownInPlayArea) Restart();
    }

    public bool Owns(PlaneSkin plane)
    {
        if (plane == null) return false;
        return plane.price <= 0 || PlayerPrefs.GetInt(OwnedPlaneKeyPrefix + plane.id, 0) == 1;
    }

    /// <summary>Paga o avião com a carteira. Falso se faltam moedas.</summary>
    public bool Buy(PlaneSkin plane)
    {
        if (plane == null || State != GameState.Ready) return false;
        if (Owns(plane)) return true;
        if (TotalCoins < plane.price) return false;

        TotalCoins -= plane.price;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.SetInt(OwnedPlaneKeyPrefix + plane.id, 1);
        // compra é dinheiro: grava já, não no fim da partida
        PlayerPrefs.Save();

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        TotalCoinsChanged?.Invoke(TotalCoins);
        return true;
    }

    /// <summary>Troca de avião. Só aceita um que já esteja pago.</summary>
    public bool Select(PlaneSkin plane)
    {
        if (!Owns(plane) || State != GameState.Ready) return false;
        if (plane == SelectedPlane) return true;

        SelectedPlane = plane;
        PlayerPrefs.SetString(SelectedPlaneKey, plane.id);
        PlayerPrefs.Save();

        PlaneChanged?.Invoke(plane);
        return true;
    }

    public void StartGame()
    {
        if (State != GameState.Ready) return;
        State = GameState.Playing;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayStart();
        StateChanged?.Invoke(State);
    }

    public void AddScore()
    {
        if (State != GameState.Playing) return;
        Score++;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayScore();
        ScoreChanged?.Invoke(Score);
    }

    public void CollectCoin()
    {
        if (State != GameState.Playing) return;
        Coins++;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin();
        CoinsChanged?.Invoke(Coins);
    }

    public void PigeonStrike()
    {
        if (State != GameState.Playing) return;

        lastPigeonTime = Time.time;
        PigeonStruck?.Invoke();
    }

    /// <param name="hitLightning">Quem chama diz no que é que o avião bateu.</param>
    public void GameOver(bool hitLightning = false)
    {
        if (State == GameState.GameOver) return;

        State = GameState.GameOver;
        gameOverTime = Time.unscaledTime;
        HitLightning = hitLightning;
        // Time.time e não tempo real: a pausa do embate, que vem antes daqui,
        // anda em câmara lenta e não pode comer a janela da culpa
        BlamePigeon = Time.time - lastPigeonTime <= config.pigeonBlameWindow;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayCrash(hitLightning);

        NewRecord = Score > HighScore;
        if (NewRecord)
        {
            HighScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
        }

        TotalCoins += Coins;
        PlayerPrefs.SetInt(TotalCoinsKey, TotalCoins);
        PlayerPrefs.Save();

        StateChanged?.Invoke(State);
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
