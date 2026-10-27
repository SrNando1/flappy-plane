using UnityEngine;

/// <summary>
/// Backend usado no Editor e quando o plugin da AdMob não está instalado.
/// Não carrega nada: só liga e desliga um retângulo na UI, para se ver
/// exatamente que espaço o banner real vai ocupar.
/// </summary>
public class FakeAdsBackend : IAdsBackend
{
    readonly GameObject placeholder;

    public FakeAdsBackend(GameObject placeholder)
    {
        this.placeholder = placeholder;
    }

    public void Initialize(string bannerUnitId)
    {
        Debug.Log("[Ads] Backend de simulação ativo. Nenhum anúncio real será pedido.");
        HideBanner();
    }

    public void ShowBanner()
    {
        if (placeholder != null) placeholder.SetActive(true);
    }

    public void HideBanner()
    {
        if (placeholder != null) placeholder.SetActive(false);
    }

    public void Dispose()
    {
    }
}
