using AppsFlyerSDK;
using DG.Tweening;
using GoogleMobileAds.Api;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GoogleMobileAds.Api.AdValue;

public class NativeBannerAdView : MonoBehaviour
{
    private float _timeRequestNative = -999;
    private string _adUnitID;
    private long _bannerNativeRefreshTime;

    public AndroidJavaObject Activity => NativeBannerController.Instance.Activity;
    public AndroidJavaObject AdManager => NativeBannerController.Instance.AdManager;


    public void Init(string adUnitID, long bannerNativeRefreshTime)
    {
        _adUnitID = adUnitID;
        _bannerNativeRefreshTime = bannerNativeRefreshTime;
    }

    private void OnEnable()
    {
        StartCoroutine(RequestNativeAdsLoop());
    }

    private IEnumerator RequestNativeAdsLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0, (_timeRequestNative + _bannerNativeRefreshTime) - Time.time));

        while (true)
        {
            RequestNativeAd();
            yield return new WaitForSeconds(_bannerNativeRefreshTime);
        }
    }

    public void RequestNativeAd()
    {
        _timeRequestNative = Time.time;
        if (Application.platform == RuntimePlatform.Android)
        {
            AdManager.Call("loadNativeAd", Activity, _adUnitID);
        }
        AnalyticsManager.Instance.LogEvent("ad_banner_request");
    }
}
