using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;
using System;

public class NativeAdsUI : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private GameObject _loading;

    [SerializeField] private RawImage _icon;
    [SerializeField] private RawImage _adChoices;

    [SerializeField] TextMeshProUGUI _adHeadline;
    [SerializeField] TextMeshProUGUI _adCallToAction;
    [SerializeField] TextMeshProUGUI _adAdvertiser;

    private bool nativeAdLoaded;
    private NativeAd nativeAd;
    private BoxCollider[] _colliders;

    private void Start()
    {
        if (AdsManager.Instance.IsRemoveAds() == false && (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.native_ad_on_off))
            RequestNativeAd();

        _content.SetActive(false);
        _loading.SetActive(true);

        _colliders = GetComponentsInChildren<BoxCollider>(true);

        //AdsManager.OnNativeAdsClickEnabled += EnableClick;
    }

    private void OnDestroy()
    {
        //AdsManager.OnNativeAdsClickEnabled -= EnableClick;
    }

    private string GetID()
    {
#if UNITY_ANDROID
        return (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.NATIVE_AD_UNIT_ID);
#elif UNITY_IOS
        return (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.NATIVE_AD_UNIT_ID_IOS);
#endif
    }

    private void EnableClick(bool enabled)
    {
        foreach (var collider in _colliders)
            collider.enabled = enabled;
    }

    private void RequestNativeAd()
    {
        AdLoader adLoader = new AdLoader.Builder(GetID())
            .ForNativeAd()
            .Build();
        adLoader.OnNativeAdLoaded += this.HandleNativeAdLoaded;
        adLoader.OnNativeAdImpression += AdLoader_OnNativeAdImpression;
        adLoader.OnNativeAdClicked += AdLoader_OnNativeAdClicked;
        adLoader.LoadAd(new AdRequest());
    }

    private void AdLoader_OnNativeAdImpression(object sender, EventArgs e)
    {
        Debug.Log("NativeAdsUI_AdLoader_OnNativeAdImpression");
    }

    private void AdLoader_OnNativeAdClicked(object sender, EventArgs e)
    {

    }
    private void HandleNativeAdLoaded(object sender, NativeAdEventArgs e)
    {
        this.nativeAd = e.nativeAd;
        this.nativeAdLoaded = true;
    }

    private void Update()
    {
        if (this.nativeAdLoaded)
        {
            nativeAdLoaded = false;
            _content.SetActive(true);
            _loading.SetActive(false);

            _icon.texture = nativeAd.GetIconTexture();
            _adChoices.texture = nativeAd.GetAdChoicesLogoTexture();

            _adCallToAction.text = nativeAd.GetCallToActionText();
            _adHeadline.text = nativeAd.GetHeadlineText();
            _adAdvertiser.text = nativeAd.GetAdvertiserText();

            nativeAd.RegisterIconImageGameObject(_icon.gameObject);
            nativeAd.RegisterAdChoicesLogoGameObject(_adChoices.gameObject);
            nativeAd.RegisterHeadlineTextGameObject(_adHeadline.gameObject);
            nativeAd.RegisterCallToActionGameObject(_adCallToAction.gameObject);
            nativeAd.RegisterAdvertiserTextGameObject(_adAdvertiser.gameObject);
        }
    }
}
