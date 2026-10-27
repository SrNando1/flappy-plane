/// <summary>
/// Tudo o que o jogo precisa de saber sobre anúncios. Trocar de rede é
/// escrever outra implementação disto — nada mais no projeto muda.
/// </summary>
public interface IAdsBackend
{
    void Initialize(string bannerUnitId);
    void ShowBanner();
    void HideBanner();
    void Dispose();
}
