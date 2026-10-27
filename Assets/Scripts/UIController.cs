using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [SerializeField] Text scoreText;
    [SerializeField] Text bestText;
    [SerializeField] Text coinsText;

    [Tooltip("A carteira, por baixo do recorde — só se vê na tela inicial.")]
    [SerializeField] Text totalCoinsText;

    [SerializeField] GameObject readyPanel;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Text finalScoreText;
    [SerializeField] Text highScoreText;
    [SerializeField] Text coinsFinalText;
    [SerializeField] Text recordBadge;

    [Tooltip("A frase de remate, por baixo dos números. Ver DeathQuips.")]
    [SerializeField] Text quipText;

    [Tooltip("O letreiro que salta quando o avião leva com um pombo.")]
    [SerializeField] Text toastText;

    [Tooltip("A loja de aviões, que o botão STORE abre.")]
    [SerializeField] StorePanel store;

    static readonly Color BestIdle = new Color(1f, 1f, 1f, 0.8f);
    static readonly Color BestBeaten = new Color32(255, 214, 66, 255);

    static readonly string[] PigeonToasts =
    {
        "BIRD STRIKE!",
        "SORRY, PIGEON.",
        "IT'S JUST A PIGEON.",
    };

    /// <summary>O letreiro: salta maior do que é, assenta, fica, e desvanece.</summary>
    const float ToastPop = 0.16f;
    const float ToastHold = 0.7f;
    const float ToastFade = 0.3f;

    GameManager game;
    Coroutine toast;
    int lastToast = -1;

    void Start()
    {
        game = GameManager.Instance;
        game.StateChanged += OnStateChanged;
        game.ScoreChanged += OnScoreChanged;
        game.CoinsChanged += OnCoinsChanged;
        game.PigeonStruck += OnPigeonStruck;
        game.TotalCoinsChanged += OnTotalCoinsChanged;

        if (toastText != null) toastText.enabled = false;

        scoreText.text = "0";
        scoreText.enabled = false;

        if (coinsText != null)
        {
            coinsText.text = "COINS  0";
            coinsText.enabled = false;
        }

        RefreshBest();

        // a carteira só se lê na tela inicial: é filha do readyPanel e não
        // muda durante a partida, por isso um valor só, aqui, chega
        if (totalCoinsText != null) totalCoinsText.text = "TOTAL COINS  " + game.TotalCoins;

        readyPanel.SetActive(true);
        gameOverPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (game == null) return;
        game.StateChanged -= OnStateChanged;
        game.ScoreChanged -= OnScoreChanged;
        game.CoinsChanged -= OnCoinsChanged;
        game.PigeonStruck -= OnPigeonStruck;
        game.TotalCoinsChanged -= OnTotalCoinsChanged;
    }

    /// <summary>Uma compra na loja: a carteira da tela inicial acompanha.</summary>
    void OnTotalCoinsChanged(int total)
    {
        if (totalCoinsText != null) totalCoinsText.text = "TOTAL COINS  " + total;
    }

    void OnCoinsChanged(int coins)
    {
        if (coinsText != null) coinsText.text = "COINS  " + coins;
    }

    void OnPigeonStruck()
    {
        if (toastText == null) return;

        // nunca a mesma frase duas vezes seguidas, pelo motivo do DeathQuips
        int pick = Random.Range(0, PigeonToasts.Length);
        if (pick == lastToast) pick = (pick + 1) % PigeonToasts.Length;
        lastToast = pick;

        if (toast != null) StopCoroutine(toast);
        toast = StartCoroutine(ShowToast(PigeonToasts[pick]));
    }

    /// <summary>O botão STORE da tela inicial: abre a loja de aviões.</summary>
    public void OnStorePressed()
    {
        if (store != null) store.Open();
    }

    /// <summary>
    /// Tempo de jogo, não real: na pausa do embate o letreiro congela com o
    /// resto do mundo, em vez de continuar a desvanecer por cima dos olhos.
    /// </summary>
    IEnumerator ShowToast(string message)
    {
        toastText.text = message;
        toastText.enabled = true;

        var rt = toastText.rectTransform;
        var color = toastText.color;
        float age = 0f;

        while (age < ToastPop + ToastHold + ToastFade)
        {
            age += Time.deltaTime;

            // passa de 1 a meio do salto e volta a assentar: um carimbo, não um fade-in
            float pop = Mathf.Clamp01(age / ToastPop);
            float scale = pop < 1f ? Mathf.Sin(pop * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) : 1f;
            rt.localScale = Vector3.one * Mathf.Max(scale, 0.01f);

            float fade = Mathf.InverseLerp(ToastPop + ToastHold, ToastPop + ToastHold + ToastFade, age);
            toastText.color = new Color(color.r, color.g, color.b, 1f - fade);

            yield return null;
        }

        toastText.enabled = false;
        toastText.color = color;
        rt.localScale = Vector3.one;
        toast = null;
    }

    void OnScoreChanged(int score)
    {
        scoreText.text = score.ToString();
        RefreshBest();
    }

    /// <summary>
    /// O recorde no canto. O <see cref="GameManager"/> só grava o novo recorde
    /// no fim da partida, mas deixar o canto a mostrar um número menor do que o
    /// placar parecia avaria: mal a pontuação passa o recorde, é ela que aparece
    /// ali, a dourado.
    /// </summary>
    void RefreshBest()
    {
        bool beaten = game.Score > game.HighScore;

        bestText.text = "HIGH SCORE  " + Mathf.Max(game.HighScore, game.Score);
        bestText.color = beaten ? BestBeaten : BestIdle;
    }

    void OnStateChanged(GameState state)
    {
        readyPanel.SetActive(state == GameState.Ready);
        if (state != GameState.Ready && store != null) store.gameObject.SetActive(false);

        if (state == GameState.Playing)
        {
            scoreText.enabled = true;
            if (coinsText != null) coinsText.enabled = true;
            return;
        }

        if (state == GameState.GameOver) StartCoroutine(ShowGameOver());
    }

    /// <summary>
    /// Espera a animação de queda terminar antes de cobrir o ecrã.
    /// O placar fica visível durante a explosão — é o número que o jogador
    /// quer ver enquanto o avião se desfaz.
    /// </summary>
    IEnumerator ShowGameOver()
    {
        yield return new WaitForSeconds(game.Config.crashDuration);

        scoreText.enabled = false;
        // o painel já traz o recorde ao centro: dois no ecrã era repetição
        bestText.enabled = false;
        if (coinsText != null) coinsText.enabled = false;

        gameOverPanel.SetActive(true);
        finalScoreText.text = "SCORE  " + game.Score;
        highScoreText.text = "HIGH SCORE  " + game.HighScore;
        recordBadge.enabled = game.NewRecord;

        if (coinsFinalText != null)
        {
            coinsFinalText.text = "COINS  " + game.Coins + "   TOTAL  " + game.TotalCoins;
        }

        if (quipText != null)
        {
            quipText.text = DeathQuips.For(game.Score, game.HighScore, game.NewRecord, game.HitLightning, game.BlamePigeon);
        }

        if (game.NewRecord && AudioManager.Instance != null) AudioManager.Instance.PlayRecord();
    }
}
