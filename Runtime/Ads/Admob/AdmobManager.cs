using AppsFlyerSDK;
using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GoogleMobileAds.Api.AdValue;

public class AdmobManager : MonoBehaviour
{
    [SerializeField] private AdmobBannerLoader _collapsibleLoader;
    [SerializeField] private AdmobBannerLoader _mrecLoader;

    public static AdmobManager Instance;

#if UNITY_ANDROID
    private string InterstitialAdUnitId = "ca-app-pub-2913496970595341/4437659255";
    private const string RewardedAdUnitId = "ca-app-pub-2913496970595341/9410944717";
#elif UNITY_IOS || UNITY_IPHONE
    private string InterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    private const string RewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private const string InterstitialAdUnitId = "unused";
    private const string RewardedAdUnitId = "unused";
#endif

    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;

    private bool _isInitialized;

    private bool _isBannerShowing = false;
    private bool _isCollapsibleShow;

    private Action _callBackReward;
    private Action _callBackInters;

    private int _interstitialRetryAttempt;
    private int _rewardedRetryAttempt;

    private float _timeCloseAds = -999;
    private string _intersPlacement;
    private string _rewardPlacement;

    private AdPosition _lastMrecPosition;
    private bool _isInterstitialNormal;

    public static event Action OnShowMREC;
    public bool IsMrecShow { get; set; }
    public bool IsMrecLoaded { get; set; }
    public float TimeCloseAds { get => _timeCloseAds; set => _timeCloseAds = value; }
    public bool IsCanShowAOAWhenBackToGame { get => AdsManager.Instance.IsCanShowAOAWhenBackToGame; set => AdsManager.Instance.IsCanShowAOAWhenBackToGame = value; }

    public AdmobBannerLoader CollapsibleLoader => _collapsibleLoader;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InterstitialAdUnitId = (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.INTER_IMAGE_AD_UNIT_ID);
        _collapsibleLoader.SetID((string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.BANNER_AD_UNIT_ID_COLLAPSIBLE));
        //_mrecLoader.SetID((string)RemoteConfigControl.Instance.GetValue(EnumRemoteVarible.MREC_AD_UNIT_ID));
    }

    public void Init()
    {
        if (_isInitialized) return;

        _mrecLoader.OnLoadSuccess = () =>
        {
            IsMrecLoaded = true;
        };

        _collapsibleLoader.OnLoadFail = () =>
        {
            if (_isBannerShowing == false) return;

            _isCollapsibleShow = false;
            _collapsibleLoader.HideBanner();
            AdsManager.Instance.ShowBanner();
        };

        _collapsibleLoader.OnLoadSuccess = () =>
        {
            if (_isBannerShowing == false) return;

            _isCollapsibleShow = true;
            AdsManager.Instance.HideBanner();
            _collapsibleLoader.ShowBanner();
            StartCoroutine(IE_DelayShowMaxBanner(_collapsibleLoader.IsCollapsible()));
        };

        _isInitialized = true;
        _lastMrecPosition = AdPosition.Bottom;
        //_mrecLoader.LoadBanner(AdSize.MediumRectangle, _lastMrecPosition, false);

        //LoadInterstitial();
        //LoadRewarded();
        AdmobAOAManager.I.LoadAd();
    }

    #region Banner

    public void ReloadBanner()
    {
        if (_isBannerShowing == false) return;
        ShowBannerCustomRefresh();
    }

    public void ShowBanner()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;
        _isBannerShowing = true;
        ShowBannerCustomRefresh();
    }

    public void ShowBannerSmall()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;
        _isBannerShowing = true;
        AdsManager.Instance.ShowBanner();
    }

    private void ShowBannerCustomRefresh()
    {
        StopAllCoroutines();
        if ((bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.collapsible_ad_on_off))
            StartCoroutine(IE_ShowBannerCustomRefresh());
        else AdsManager.Instance.ShowBanner();
    }

    private IEnumerator IE_ShowBannerCustomRefresh()
    {
        yield return new WaitUntil(() => _isInitialized);
        while (true)
        {
            if (AdsManager.Instance.IsMrecShow == false)
            {
                AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
                _collapsibleLoader.LoadBanner(adaptiveSize, AdPosition.Bottom, true);
                if (_isCollapsibleShow)
                    _collapsibleLoader.ShowBanner();
            }
            long timeWait = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_refresh_banner);
            yield return new WaitForSecondsRealtime(timeWait);
        }
    }

    private IEnumerator IE_DelayShowMaxBanner(bool isCollapsible)
    {
        long timeWait;
        if (isCollapsible)
            timeWait = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_auto_close_collap_banner);
        else
            timeWait = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_auto_close_normal_banner);

        yield return new WaitForSecondsRealtime(timeWait);
        if (_isCollapsibleShow == false) yield break;
        _isCollapsibleShow = false;
        _collapsibleLoader.DestroyBanner();
        AdsManager.Instance.ShowBanner();
    }

    public void HideBanner()
    {
        _isBannerShowing = false;
        StopAllCoroutines();
        _collapsibleLoader.HideBanner();
        _collapsibleLoader.DestroyBanner();
        AdsManager.Instance.HideBanner();
    }

    #endregion

    #region MREC

    public void ShowMREC()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;

        if ((bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.mrec_ad_on_off) == false)
            return;
        OnShowMREC?.Invoke();
        _mrecLoader.ShowBanner();
        IsMrecShow = true;
    }

    public void ShowMREC(AdPosition adPosition)
    {
        if (AdsManager.Instance.IsRemoveAds()) return;

        if ((bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.mrec_ad_on_off) == false)
            return;

        if (_lastMrecPosition != adPosition)
        {
            _mrecLoader.DestroyBanner();
            _mrecLoader.LoadBanner(AdSize.MediumRectangle, adPosition, false);
        }
        _lastMrecPosition = adPosition;
        ShowMREC();
    }

    public void HideMREC()
    {
        IsMrecShow = false;
        _mrecLoader.HideBanner();
    }
    #endregion

    #region Interstitial

    public void ChangeInterstitailToNormalID()
    {
        _isInterstitialNormal = true;
        InterstitialAdUnitId = "ca-app-pub-2913496970595341/9709880501";
        LoadInterstitial();
    }

    public bool IsCanShowInterstitial()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
            return true;
        return false;
    }

    /// <summary>
    /// Loads the ad.
    /// </summary>
    private void LoadInterstitial()
    {
        if (_isInitialized == false) return;

        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            DestroyInterstitial();
        }

        Debug.Log("Loading interstitial ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        InterstitialAd.Load(InterstitialAdUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                _interstitialRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
                Invoke(nameof(LoadInterstitial), (float)retryDelay);
                Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                return;
            }
            // If the operation failed for unknown reasons.
            // This is an unexpected error, please report this bug if it happens.
            if (ad == null)
            {
                _interstitialRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
                Invoke(nameof(LoadInterstitial), (float)retryDelay);
                Debug.LogError("Unexpected error: Interstitial load event fired with null ad and null error.");
                return;
            }

            _interstitialRetryAttempt = 0;
            // The operation completed successfully.
            Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
            _interstitialAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlersInterstitial(ad);
        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowInterstitial(Action actionDone, string placement, bool ignoreCapping = false)
    {
        if (AdsManager.Instance.IsRemoveAds()) return;

        bool interAdOnOff = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_on_off_user_organic);
        if (AttributionManager.IsUserOrganic == false)
            interAdOnOff = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_on_off_user_paid);

        if (!interAdOnOff)
        {
            actionDone?.Invoke();
            return;
        }

        _intersPlacement = placement;
        if (_isInterstitialNormal)
        {
            AppsFlyer.sendEvent("af_inters_call_show", null);
            AnalyticsManager.Instance.LogAdsEvent("inters_call_show", _intersPlacement);
        }
        else
        {
            AppsFlyer.sendEvent("af_inters_image_call_show", null);
            AnalyticsManager.Instance.LogAdsEvent("inters_image_call_show", _intersPlacement);
        }
        IsCanShowAOAWhenBackToGame = false;
        bool isEnoughCapping = Time.unscaledTime >= (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_capping_time_user_organic) + _timeCloseAds;
        if ((isEnoughCapping || ignoreCapping) && AdsManager.Instance.IsRemoveAds() == false)
        {
            if (_isInterstitialNormal)
            {
                AppsFlyer.sendEvent("af_inters_passed_capping_time", null);
                AnalyticsManager.Instance.LogAdsEvent("inters_passed_capping_time", _intersPlacement);
            }
            else
            {
                AppsFlyer.sendEvent("af_inters_image_passed_capping_time", null);
                AnalyticsManager.Instance.LogAdsEvent("inters_image_passed_capping_time", _intersPlacement);
            }
            if (_interstitialAd != null && _interstitialAd.CanShowAd())
            {
                if (_isInterstitialNormal)
                {
                    AppsFlyer.sendEvent("af_inters_available", null);
                    AnalyticsManager.Instance.LogAdsEvent("inters_available", _intersPlacement);
                }
                else
                {
                    AppsFlyer.sendEvent("af_inters_image_available", null);
                    AnalyticsManager.Instance.LogAdsEvent("inters_image_available", _intersPlacement);
                }
                Debug.Log("Showing interstitial ad.");
                _callBackInters = actionDone;
                _interstitialAd.Show();
            }
            else
            {
                LoadInterstitial();
                Debug.LogError("Interstitial ad is not ready yet.");
                _callBackInters?.Invoke();
                _callBackInters = null;
            }
        }
        else
        {
            IsCanShowAOAWhenBackToGame = true;
            actionDone.Invoke();
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyInterstitial()
    {
        if (_interstitialAd != null)
        {
            Debug.Log("Destroying interstitial ad.");
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }
    }

    private void RegisterEventHandlersInterstitial(InterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));

            AdmobManager.Instance.LogAdRevenueAdmob(adValue, ad.GetResponseInfo());
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
            if (_isInterstitialNormal)
            {
                AppsFlyer.sendEvent("af_inters_displayed", null);
                AnalyticsManager.Instance.LogAdsEvent("inters_displayed", _intersPlacement);
            }
            else
            {
                AppsFlyer.sendEvent("af_inters_image_displayed", null);
                AnalyticsManager.Instance.LogAdsEvent("inters_image_displayed", _intersPlacement);
            }
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");

            if (_callBackInters != null)
            {
                _callBackInters.Invoke();
                _callBackInters = null;
                _timeCloseAds = Time.unscaledTime;
            }
            IsCanShowAOAWhenBackToGame = true;
            Invoke(nameof(LoadInterstitial), 0.5f);
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content with error : "
                + error);
            if (_callBackInters != null)
            {
                _callBackInters.Invoke();
                _callBackInters = null;
            }
            IsCanShowAOAWhenBackToGame = true;
            Invoke(nameof(LoadInterstitial), 0.5f);
        };
    }
    #endregion

    #region Rewarded
    private void LoadRewarded()
    {
        if (_isInitialized == false) return;

        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            DestroyRewarded();
        }

        Debug.Log("Loading rewarded ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        RewardedAd.Load(RewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                _rewardedRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
                Invoke(nameof(LoadRewarded), (float)retryDelay);
                Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                return;
            }
            // If the operation failed for unknown reasons.
            // This is an unexpected error, please report this bug if it happens.
            if (ad == null)
            {
                _rewardedRetryAttempt++;
                double retryDelay = Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
                Invoke(nameof(LoadRewarded), (float)retryDelay);
                Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                return;
            }

            _rewardedRetryAttempt = 0;
            // The operation completed successfully.
            Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
            _rewardedAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlersRewarded(ad);
        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowRewarded(Action callBack, string placement)
    {
        _rewardPlacement = placement;
#if ENABLE_REMOVE_ADS
        callBack?.Invoke();
        return;
#endif
        IsCanShowAOAWhenBackToGame = false;
        AppsFlyer.sendEvent("af_rewarded_call_show", null);
        AnalyticsManager.Instance.LogAdsEvent("rewarded_call_show", _rewardPlacement);
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            AppsFlyer.sendEvent("af_rewarded_available", null);
            AnalyticsManager.Instance.LogAdsEvent("rewarded_available", _rewardPlacement);
            Debug.Log("Showing rewarded ad.");
            _rewardedAd.Show((Reward reward) =>
            {
                callBack?.Invoke();
                IsCanShowAOAWhenBackToGame = true;

                AppsFlyer.sendEvent("af_rewarded_ad_completed", null);
                AnalyticsManager.Instance.LogAdsEvent("rewarded_ad_completed", _rewardPlacement);

                Debug.Log(String.Format("Rewarded ad granted a reward: {0} {1}",
                                        reward.Amount,
                                        reward.Type));
            });
        }
        else
        {
            LoadRewarded();
            Debug.LogError("Rewarded ad is not ready yet.");
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyRewarded()
    {
        if (_rewardedAd != null)
        {
            Debug.Log("Destroying rewarded ad.");
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
    }

    private void RegisterEventHandlersRewarded(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));

            AdmobManager.Instance.LogAdRevenueAdmob(adValue, ad.GetResponseInfo());
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
            AppsFlyer.sendEvent("af_rewarded_ad_displayed", null);
            AnalyticsManager.Instance.LogAdsEvent("rewarded_ad_displayed", _rewardPlacement);
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when the ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
            _timeCloseAds = Time.unscaledTime;
            Invoke(nameof(LoadRewarded), 0.5f);
            IsCanShowAOAWhenBackToGame = true;
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content with error : "
                + error);
            Invoke(nameof(LoadRewarded), 0.5f);
            IsCanShowAOAWhenBackToGame = true;
        };
    }
    #endregion


    public void LogAdRevenueAdmob(AdValue adValue, ResponseInfo responseInfo)
    {
        long valueMicros = adValue.Value;
        string currencyCode = adValue.CurrencyCode;
        PrecisionType precision = adValue.Precision;

        string responseId = "";
        string adSourceId = "";
        string adSourceName = "";

        if (responseInfo != null)
        {
            responseId = responseInfo.GetResponseId();
            AdapterResponseInfo loadedAdapterResponseInfo = responseInfo.GetLoadedAdapterResponseInfo();
            adSourceId = loadedAdapterResponseInfo.AdSourceId;
            adSourceName = loadedAdapterResponseInfo.AdSourceName;
        }

        double revenue = valueMicros / 1000000f;
        var impressionParameters = new[] {
            new Firebase.Analytics.Parameter("ad_platform", "Admob"),
            new Firebase.Analytics.Parameter("ad_source_id", adSourceId),
            new Firebase.Analytics.Parameter("ad_source_name", adSourceName),
            new Firebase.Analytics.Parameter("value", revenue),
            new Firebase.Analytics.Parameter("precision", precision.ToString()),
            new Firebase.Analytics.Parameter("currency", currencyCode),
        };
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);

        Dictionary<string, string> additionalParameters = new Dictionary<string, string>
        {
            { "ad_platform", "Admob" },
            { "ad_source_id",adSourceId},
            { "ad_source_name", adSourceName },
            { "value", revenue.ToString() },
            { "precision", precision.ToString() },
            { "currency", currencyCode }
        };

        var logRevenue = new AFAdRevenueData("Admob", MediationNetwork.GoogleAdMob, "USD", revenue);
        AppsFlyer.logAdRevenue(logRevenue, additionalParameters);
    }
}