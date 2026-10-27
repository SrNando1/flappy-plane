// O corpo desta classe só existe quando o símbolo ADMOB está definido.
// Quem o liga é Assets/Editor/AdMobDefine.cs, ao detetar o plugin instalado.
// Sem isso, o projeto compila na mesma e o jogo usa o FakeAdsBackend.
#if ADMOB
using GoogleMobileAds.Api;
using UnityEngine;

public class AdMobBackend : IAdsBackend
{
    BannerView banner;
    string unitId;
    bool wantVisible;
    bool ready;

    static bool sdkInitialized;

    public void Initialize(string bannerUnitId)
    {
        unitId = bannerUnitId;

        if (sdkInitialized)
        {
            CreateBanner();
            return;
        }

        MobileAds.Initialize(status =>
        {
            sdkInitialized = true;
            Debug.Log("[Ads] SDK da AdMob pronto.");
            CreateBanner();
        });
    }

    void CreateBanner()
    {
        Dispose();

        banner = new BannerView(unitId, AdSize.Banner, AdPosition.Bottom);

        banner.OnBannerAdLoaded += () =>
        {
            ready = true;
            Apply();
        };

        banner.OnBannerAdLoadFailed += error =>
        {
            ready = false;
            Debug.LogWarning("[Ads] Banner não carregou: " + error);
        };

        // Em versões do plugin anteriores à 8.x isto era:
        // banner.LoadAd(new AdRequest.Builder().Build());
        banner.LoadAd(new AdRequest());
    }

    public void ShowBanner()
    {
        wantVisible = true;
        Apply();
    }

    public void HideBanner()
    {
        wantVisible = false;
        Apply();
    }

    void Apply()
    {
        if (banner == null || !ready) return;

        if (wantVisible) banner.Show();
        else banner.Hide();
    }

    public void Dispose()
    {
        if (banner == null) return;

        banner.Destroy();
        banner = null;
        ready = false;
    }
}
#endif
