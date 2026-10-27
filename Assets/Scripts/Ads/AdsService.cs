using UnityEngine;

/// <summary>
/// Único ponto de contacto do jogo com anúncios. Sobrevive ao recarregar da
/// cena para o banner não ser pedido de novo a cada partida.
/// </summary>
public class AdsService : MonoBehaviour
{
    /// <summary>ID de banner de teste da Google. Serve em qualquer aparelho, sem conta.</summary>
    public const string TestBannerAndroid = "ca-app-pub-3940256099942544/6300978111";

    public static AdsService Instance { get; private set; }

    [Header("Comportamento")]
    [Tooltip("Ligado, o banner fica visível também durante a partida. " +
             "Desligado, aparece só no ecrã inicial e no fim de jogo.")]
    [SerializeField] bool showDuringGameplay;

    [Header("Identificadores")]
    [Tooltip("Enquanto ligado, usa o ID de teste da Google. " +
             "DESLIGAR SÓ NA VERSÃO PUBLICADA — clicar nos próprios anúncios reais dá banimento.")]
    [SerializeField] bool useTestAds = true;

    [Tooltip("ID da unidade de banner, do painel da AdMob. Só é usado com 'Use Test Ads' desligado.")]
    [SerializeField] string androidBannerUnitId = "ca-app-pub-3940256099942544/6300978111";

    [Tooltip("Fração da altura do ecrã, a contar de baixo, que deixa de aceitar " +
             "toques enquanto o banner está visível.")]
    [SerializeField, Range(0f, 0.3f)] float safeAreaFraction = 0.16f;

    [Header("Simulação")]
    [Tooltip("Retângulo na UI que ocupa o lugar do banner quando não há plugin.")]
    [SerializeField] GameObject placeholder;

    IAdsBackend backend;
    bool bannerVisible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        backend = CreateBackend();
        backend.Initialize(useTestAds ? TestBannerAndroid : androidBannerUnitId);
    }

    IAdsBackend CreateBackend()
    {
        // No Editor não há anúncios reais mesmo com o plugin instalado,
        // por isso a simulação é sempre mais útil aqui.
#if ADMOB && !UNITY_EDITOR
        return new AdMobBackend();
#else
        return new FakeAdsBackend(placeholder);
#endif
    }

    void Update()
    {
        // sondagem em vez de subscrição: o GameManager é recriado a cada
        // recarga da cena, e este objeto não
        var game = GameManager.Instance;
        bool shouldShow = game == null || showDuringGameplay || game.State != GameState.Playing;

        SetBanner(shouldShow);
    }

    void SetBanner(bool visible)
    {
        if (visible == bannerVisible) return;
        bannerVisible = visible;

        // enquanto o banner está no ecrã, a faixa de baixo deixa de reiniciar o jogo
        GameInput.BannerSafeFraction = visible ? safeAreaFraction : 0f;

        if (visible) backend.ShowBanner();
        else backend.HideBanner();
    }

    void OnDestroy()
    {
        if (Instance == this) backend?.Dispose();
    }
}
