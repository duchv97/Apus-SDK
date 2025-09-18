using System;
using DG.Tweening;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

public class AdmobAOAManager : MonoBehaviour
{
    public static AdmobAOAManager I;

    // App open ads can be preloaded for up to 4 hours.
    private readonly TimeSpan TIMEOUT = TimeSpan.FromHours(4);
    private DateTime _expireTime;
    private AppOpenAd _appOpenAd;

    private void Awake()
    {
        I = this;
    }

    private string GetID()
    {
#if UNITY_ANDROID
        return (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.OPEN_AD_UNIT_ID);
#elif UNITY_IOS
        return (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.OPEN_AD_UNIT_ID_IOS);
#endif
    }

    /// <summary>
    /// Loads the ad.
    /// </summary>
    public void LoadAd()
    {
        // Clean up the old ad before loading a new one.
        if (_appOpenAd != null)
        {
            DestroyAd();
        }

        Debug.Log("Loading app open ad.");

        // Create our request used to load the ad.
        AdRequest adRequest = new AdRequest();

        // Send the request to load the ad.
        AppOpenAd.Load(GetID(), adRequest,
            (AppOpenAd ad, LoadAdError error) =>
            {
                // If the operation failed with a reason.
                if (error != null)
                {
                    Debug.LogError("App open ad failed to load an ad with error : "
                                    + error);
                    return;
                }

                // If the operation failed for unknown reasons.
                // This is an unexpected error, please report this bug if it happens.
                if (ad == null)
                {
                    Debug.LogError("Unexpected error: App open ad load event fired with " +
                                   " null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("App open ad loaded with response : " + ad.GetResponseInfo());
                _appOpenAd = ad;

                // App open ads can be preloaded for up to 4 hours.
                _expireTime = DateTime.Now + TIMEOUT;

                // Register to ad events to extend functionality.
                RegisterEventHandlers(ad);

            });
    }

    public bool IsReadyToShow()
    {
        return (_appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _expireTime);
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowAd()
    {
        // App open ads can be preloaded for up to 4 hours.
        if (_appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _expireTime)
        {
            Debug.Log("Showing app open ad.");
            _appOpenAd.Show();
        }
        else
        {
            Debug.LogError("App open ad is not ready yet.");
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyAd()
    {
        if (_appOpenAd != null)
        {
            Debug.Log("Destroying app open ad.");
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogResponseInfo()
    {
        if (_appOpenAd != null)
        {
            var responseInfo = _appOpenAd.GetResponseInfo();
            UnityEngine.Debug.Log(responseInfo);
        }
    }

    private void RegisterEventHandlers(AppOpenAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("App open ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));

            AdmobManager.Instance.LogAdRevenueAdmob(adValue, ad.GetResponseInfo());
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("App open ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("App open ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("App open ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("App open ad full screen content closed.");

            // It may be useful to load a new ad when the current one is complete.
            LoadAd();
            AdsManager.Instance.TimeCloseAOA = DateTime.UtcNow;
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("App open ad failed to open full screen content with error : "
                            + error);
        };
    }
}