using UnityEngine;
using System.Collections;
using System;
using System.Runtime.InteropServices;
using AppsFlyerSDK;
using System.Collections.Generic;
using static MaxSdkBase;
using HG.Rate;
using DG.Tweening;

#if UNITY_IOS

namespace AudienceNetwork
{
    public static class AdSettings
    {
        [DllImport("__Internal")]
        private static extern void FBAdSettingsBridgeSetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled);

        public static void SetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled)
        {
            FBAdSettingsBridgeSetAdvertiserTrackingEnabled(advertiserTrackingEnabled);
        }
    }
}

#endif

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance = null;

    [Header("Base")]
    [SerializeField] private TypeAds _typeAds;
    [Header("Banner")]
    [SerializeField] private bool _isShowBannerOnLoad;
    [SerializeField] private MaxSdkBase.BannerPosition _defaultBannerPosition = MaxSdkBase.BannerPosition.BottomCenter;
    [Header("MREC")]
    [SerializeField] private MaxSdkBase.AdViewPosition _defaultMRECPosition = MaxSdkBase.AdViewPosition.Centered;
    [Header("BackFill")]
    [SerializeField] private bool _isUseAdmobIntersBackFill;
    [SerializeField] private bool _isUseAdmobRewardBackFill;

    [Header("Others")]
    [SerializeField] private LoadingAOA _loadingAOA;

    private Action _callBackReward;
    private Action _callBackInters;

    private int _interstitialRetryAttempt;
    private int _rewardedRetryAttempt;

    private DateTime _timeStartPause;
    private float _timeCloseAds;
    private bool _isCanShowAOAWhenBackToGame;
    private bool _isMrecLoaded;

    private string _intersPlacement;
    private string _rewardPlacement;

    private bool _isShowLoadingBackToGame;

    public int CountOpenGame
    {
        get { return PlayerPrefs.GetInt("CountOpenGame", 0); }
        set { PlayerPrefs.SetInt("CountOpenGame", value); }
    }

    public bool IsCanShowInterAds
    {
        get { return PlayerPrefs.GetInt("AdsManager_IsCanShowInterAds", 0) == 1; }
        set { PlayerPrefs.SetInt("AdsManager_IsCanShowInterAds", value ? 1 : 0); }
    }

    public DateTime TimeCloseAOA { get; set; }
    public bool IsMrecShow { get; private set; }

    ////////////////////////////////////////////ID
#if UNITY_IOS
    const string BannerAdUnitId = "dd8c93463f1c0db6";
    const string InterstitialAdUnitId = "b3ec13ebea665d43";
    const string RewardedAdUnitId = "2b0815359407d10f";
    //const string MrecAdUnitId = "a997135e4686b552";
    const string AppOpenAdUnitID = "ed609d60654095a1";
#else
    const string BannerAdUnitId = "53e64f21f9d70e3d";
    const string InterstitialAdUnitId = "b15ac1200fc27475";
    const string RewardedAdUnitId = "2b6eed12b081b66d";
    const string AppOpenAdUnitID = "fabe773c9ac58eba";
#endif
    public string MrecAdUnitId
    {
#if UNITY_ANDROID
        get { return (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.MAX_MREC_AD_UNIT_ID); }
#else
        get { return (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.MREC_AD_UNIT_ID_IOS); }
#endif
    }
    ////////////////////////////////////////////ID

    public bool IsMrecLoaded { get => _isMrecLoaded; }
    public bool IsCanShowAOAWhenBackToGame { get => _isCanShowAOAWhenBackToGame; set => _isCanShowAOAWhenBackToGame = value; }
    public bool IgnoreShowAOAWhenBackToGame { get; set; }

    public static Action OnShowMREC;
    public static Action OnDisconnected;

    private bool _isCallInit;

    void Awake()
    {
        Instance = this;

        MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
        {
            // AppLovin SDK is initialized, configure and start loading ads.
            Debug.Log("MAX SDK Initialized");

#if UNITY_IOS || UNITY_IPHONE
            if (MaxSdkUtils.CompareVersions(UnityEngine.iOS.Device.systemVersion, "14.5") != MaxSdkUtils.VersionComparisonResult.Lesser)
            {
                // Note that App transparency tracking authorization can be checked via `sdkConfiguration.AppTrackingStatus` for Unity Editor and iOS targets
                // 1. Set Meta ATE flag here, THEN
                AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(true);
            }
#endif

            AppsFlyerManager.Instance.Init();
            RegisterPaidEvent();
            if (_typeAds.HasFlag(TypeAds.Inters))
                InitializeInterstitialAds();
            if (_typeAds.HasFlag(TypeAds.Rewarded))
                InitializeRewardedAds();
            if (_typeAds.HasFlag(TypeAds.Banner))
                InitializeBannerAds();
            if (_typeAds.HasFlag(TypeAds.Mrec))
                InitializeMRecAds();
#if USE_ADMOB_AOA
#else
            InitializeAppOpen();
#endif
            //CooldownAdsIngame();
            //MaxSdk.ShowMediationDebugger();
        };

        MaxSdk.SetUserId(AppsFlyer.getAppsFlyerId());
    }

    public void Init()
    {
        if (_isCallInit) return;
        _isCallInit = true;
        MaxSdk.InitializeSdk();
    }

    private void Start()
    {
        _timeCloseAds = -999f;
        CountOpenGame++;
        _isCanShowAOAWhenBackToGame = true;
        if (IsCanShowAOA())
        {
#if !USE_ADMOB_AOA
            MaxSdk.LoadAppOpenAd(AppOpenAdUnitID);
#endif
        }
    }

    #region Retry

    public void TryToReInit()
    {
        if (CheckInternet() && !MaxSdk.IsInitialized())
        {
            MaxSdk.InitializeSdk();
        }
    }
    #endregion

    #region Base Init
    private void RegisterPaidEvent()
    {
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
    }
    private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo impressionData)
    {
        double revenue = impressionData.Revenue;
        var impressionParameters = new[] {
            new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
            new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
            new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
            new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat), // Please check this - as wecouldn't find format refereced in your unity docshttps://dash.applovin.com/documentation/mediation/unity/getting-started/advanced-settings#impression-level-user-revenue - api
            new Firebase.Analytics.Parameter("placement", impressionData.Placement),
            new Firebase.Analytics.Parameter("value", revenue),
            new Firebase.Analytics.Parameter("currency", "USD"), // All Applovin revenue is sent in USD
            };
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);

        Dictionary<string, string> additionalParameters = new Dictionary<string, string>
        {
            { "ad_platform", "AppLovin" },
            { "ad_source", impressionData.NetworkName },
            { "ad_unit_name", impressionData.AdUnitIdentifier },
            { "ad_format", impressionData.AdFormat },
            { "placement", impressionData.Placement },
            { "value", impressionData.Revenue.ToString() },
            { "currency", "USD" }
        };

        var logRevenue = new AFAdRevenueData(impressionData.NetworkName, MediationNetwork.ApplovinMax, "USD", impressionData.Revenue);
        AppsFlyer.logAdRevenue(logRevenue, additionalParameters);
    }
    #endregion

    #region App Open 

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            if (IgnoreShowAOAWhenBackToGame)
            {
                IgnoreShowAOAWhenBackToGame = false;
                return;
            }

            if (!_isShowLoadingBackToGame
                && Static.IsInGame
                && IsCanShowAOAInGame()
                && IsEnoughCappingAOA()
                && (DateTime.UtcNow.Subtract(_timeStartPause).TotalSeconds >= (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_pause_to_show_AOA)))
                StartCoroutine(IE_ShowLoadingBackToGame());

            // if (IAPManager.I.IsBuyPopupShowed)
            // {
            //     _isShowLoadingBackToGame = true;
            //     IAPManager.I.IsBuyPopupShowed = false;
            // }
        }
        else
        {
            _timeStartPause = DateTime.UtcNow;
        }
    }

    private IEnumerator IE_ShowLoadingBackToGame()
    {
        _isShowLoadingBackToGame = true;
        _loadingAOA.ShowLoading(6f);
        yield return new WaitForSecondsRealtime(0.5f);
        for (int i = 0; i < 5; i++)
        {
            if (AdmobAOAManager.I.IsReadyToShow())
            {
                ShowAOA(false);
                break;
            }
            else yield return new WaitForSecondsRealtime(1f);
        }
        _loadingAOA.ForceLoadingDone(0.5f);
        yield return new WaitForSecondsRealtime(0.5f);
        _loadingAOA.HideLoading();
        _isShowLoadingBackToGame = false;
    }

    public bool IsEnoughCappingAOA()
    {
        long capping = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.open_ad_capping_time);
        return (DateTime.UtcNow.Subtract(TimeCloseAOA).TotalSeconds >= capping);
    }

    public bool IsCanShowAOAInGame()
    {
        return ((bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.open_ad_ingame_on_off) && IsRemoveAds() == false && _isCanShowAOAWhenBackToGame);
    }

    public bool IsCanShowAOA()
    {
        return ((bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.open_ad_on_off) && IsRemoveAds() == false && _isCanShowAOAWhenBackToGame);
    }

    public void ShowAOA(bool isFirstTime)
    {
        if (IsCanShowAOA() && ((isFirstTime && CountOpenGame >= 2) || (!isFirstTime)))
        {
#if USE_ADMOB_AOA
            AdmobAOAManager.I.ShowAd();
#else
            ShowAppOpen();
#endif
        }
    }

    private void InitializeAppOpen()
    {
        MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAdHiddenEvent;
    }

    private void OnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        MaxSdk.LoadAppOpenAd(AppOpenAdUnitID);
        TimeCloseAOA = DateTime.UtcNow;
    }

    private void ShowAppOpen()
    {
        if (MaxSdk.IsAppOpenAdReady(AppOpenAdUnitID))
        {
            MaxSdk.ShowAppOpenAd(AppOpenAdUnitID);
        }
        else
        {
            MaxSdk.LoadAppOpenAd(AppOpenAdUnitID);
        }
    }
    #endregion

    #region Interstitial Ad Methods

    private void InitializeInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdDisplayFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += InterstitialOnAdClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdHiddenEvent;

        MaxSdk.LoadInterstitial(InterstitialAdUnitId);

        //LoadInterstitial();
    }

    public void LoadInterstitial()
    {
        Debug.Log("Load Full Ads");
        if (MaxSdk.IsInitialized())
        {
            if (!MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
            {
                MaxSdk.LoadInterstitial(InterstitialAdUnitId);
            }
            else
            {
                Debug.Log("Load Full Ads - AdsIsReady - Not Load");
            }
        }
    }

    public void ShowInterstitial(Action actionDone, string placement, bool ignoreCapping = false)
    {
        bool interAdOnOff = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_on_off_user_organic);
        if (AttributionManager.IsUserOrganic == false)
            interAdOnOff = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_on_off_user_paid);

        if (!interAdOnOff)
        {
            actionDone?.Invoke();
            return;
        }

        _intersPlacement = placement;
        AppsFlyer.sendEvent("af_inters_call_show", null);
        AnalyticsManager.Instance.LogAdsEvent("inters_call_show", _intersPlacement);
        _isCanShowAOAWhenBackToGame = false;

        long capping = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_capping_time_user_organic);
        if (AttributionManager.IsUserOrganic == false)
            capping = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.inter_ad_capping_time_user_paid);

        bool isEnoughCapping = Time.unscaledTime >= capping + _timeCloseAds;
        if ((isEnoughCapping || ignoreCapping) && IsRemoveAds() == false && IsCanShowInterAds)
        {
            AppsFlyer.sendEvent("af_inters_passed_capping_time", null);
            AnalyticsManager.Instance.LogAdsEvent("inters_passed_capping_time", _intersPlacement);
            if (MaxSdk.IsInterstitialReady(InterstitialAdUnitId))
            {
                AppsFlyer.sendEvent("af_inters_available", null);
                AnalyticsManager.Instance.LogAdsEvent("inters_available", _intersPlacement);
                Debug.Log("ad ready, show ad");
                _callBackInters = actionDone;
                MaxSdk.ShowInterstitial(InterstitialAdUnitId);
            }
            else
            {
                LoadInterstitial();
                _isCanShowAOAWhenBackToGame = true;

                if (_isUseAdmobIntersBackFill)
                {
                    AdmobManager.Instance.ShowInterstitial(() =>
                    {
                        actionDone?.Invoke();
                        _timeCloseAds = Time.unscaledTime;
                    }, placement);
                }
                else
                {
                    actionDone.Invoke();
                }
            }
        }
        else
        {
            _isCanShowAOAWhenBackToGame = true;
            actionDone.Invoke();
        }
    }

    private void InterstitialOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
        _interstitialRetryAttempt = 0;
    }

    private void InterstitialOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        _interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _interstitialRetryAttempt));
        //Invoke(nameof(LoadInterstitial), (float)retryDelay);
        DOVirtual.DelayedCall((float)retryDelay, LoadInterstitial).SetUpdate(true);
    }

    private void InterstitialOnAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
    {
        AppsFlyer.sendEvent("af_inters_displayed", null);
        AnalyticsManager.Instance.LogAdsEvent("inters_displayed", _intersPlacement);
    }

    private void InterstitialOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad failed to display. We recommend loading the next ad
        if (_callBackInters != null)
        {
            _callBackInters.Invoke();
            _callBackInters = null;
        }
        _isCanShowAOAWhenBackToGame = true;
        //Invoke(nameof(LoadInterstitial), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadInterstitial).SetUpdate(true);
    }

    private void InterstitialOnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
    {
    }

    private void InterstitialOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is hidden. Pre-load the next ad
        if (_callBackInters != null)
        {
            _callBackInters.Invoke();
            _callBackInters = null;
        }
        _timeCloseAds = Time.unscaledTime;
        _isCanShowAOAWhenBackToGame = true;
        //Invoke(nameof(LoadInterstitial), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadInterstitial).SetUpdate(true);
    }

    #endregion

    #region Rewarded Ad Methods

    private void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += RewardedOnAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += RewardedOnAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedOnAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedOnAdDisplayFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += RewardedOnAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedOnAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedOnAdReceivedRewardEvent;

        // Load the first RewardedAd
        //Invoke(nameof(LoadRewardedAd), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadRewardedAd).SetUpdate(true);

    }

    private void LoadRewardedAd()
    {
        if (MaxSdk.IsInitialized())
        {
            MaxSdk.LoadRewardedAd(RewardedAdUnitId);
        }
    }

    public void ShowRewarded(Action callBack, string placement)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            OnDisconnected?.Invoke();
            return;
        }

        _rewardPlacement = placement;
#if ENABLE_REMOVE_ADS
        callBack?.Invoke();
        return;
#else
        _isCanShowAOAWhenBackToGame = false;
        AppsFlyer.sendEvent("af_rewarded_call_show", null);
        AnalyticsManager.Instance.LogAdsEvent("rewarded_call_show", _rewardPlacement);
        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitId))
        {
            AppsFlyer.sendEvent("af_rewarded_available", null);
            AnalyticsManager.Instance.LogAdsEvent("rewarded_available", _rewardPlacement);
            _callBackReward = callBack;
            MaxSdk.ShowRewardedAd(RewardedAdUnitId);
        }
        else
        {
            if (_isUseAdmobRewardBackFill)
                AdmobManager.Instance.ShowRewarded(() =>
                {
                    callBack?.Invoke();
                    _timeCloseAds = Time.unscaledTime;
                }, placement);
        }
#endif
    }

    private void RewardedOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
        Debug.Log("Rewarded ad loaded");
        // Reset retry attempt
        _rewardedRetryAttempt = 0;
    }

    private void RewardedOnAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
        _rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
        //Invoke(nameof(LoadRewardedAd), (float)retryDelay);
        DOVirtual.DelayedCall((float)retryDelay, LoadRewardedAd).SetUpdate(true);
    }

    private void RewardedOnAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. We recommend loading the next ad
        //Invoke(nameof(LoadRewardedAd), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadRewardedAd).SetUpdate(true);
        _isCanShowAOAWhenBackToGame = true;
    }

    private void RewardedOnAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        AppsFlyer.sendEvent("af_rewarded_ad_displayed", null);
        AnalyticsManager.Instance.LogAdsEvent("rewarded_ad_displayed", _rewardPlacement);
    }

    private void RewardedOnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        Debug.Log("Rewarded ad clicked");
    }

    private void RewardedOnAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        _timeCloseAds = Time.unscaledTime;
        //Invoke(nameof(LoadRewardedAd), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadRewardedAd).SetUpdate(true);
        _isCanShowAOAWhenBackToGame = true;
    }

    private void RewardedOnAdReceivedRewardEvent(string adUnitId, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad was displayed and user should receive the reward
        if (_callBackReward != null)
        {
            _callBackReward.Invoke();
        }
        _isCanShowAOAWhenBackToGame = true;
        AppsFlyer.sendEvent("af_rewarded_ad_completed", null);
        AnalyticsManager.Instance.LogAdsEvent("rewarded_ad_completed", _rewardPlacement);
    }

    #endregion

    #region Banner Ad Methods

    private void InitializeBannerAds()
    {
        MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
        MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdLoadFailedEvent;
        MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;

        //Invoke(nameof(LoadBanner), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadBanner).SetUpdate(true);
    }

    private void LoadBanner()
    {
        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets.
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments.
        MaxSdk.CreateBanner(BannerAdUnitId, _defaultBannerPosition);
        MaxSdk.SetBannerExtraParameter(BannerAdUnitId, "adaptive_banner", "false");
        // Set background or background color for banners to be fully functional.
        //MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
        MaxSdk.SetBannerWidth(BannerAdUnitId, (float)Screen.width);

        if (_isShowBannerOnLoad)
            ShowBanner(_defaultBannerPosition);
    }

    public void ShowBanner(MaxSdkBase.BannerPosition viewPosition)
    {
        MaxSdk.UpdateBannerPosition(BannerAdUnitId, viewPosition);
        ShowBanner();
    }

    public void ShowBanner()
    {
        if (!IsRemoveAds() && (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.banner_ad_on_off))
        {
            //NativeBannerController.Instance.ShowNativeBanner();
            MaxSdk.ShowBanner(BannerAdUnitId);
        }
    }

    public void HideBanner()
    {
        //NativeBannerController.Instance.HideNativeBanner();
        MaxSdk.HideBanner(BannerAdUnitId);
    }

    private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo) { }

    private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    #endregion

    #region MRec
    public void InitializeMRecAds()
    {
        MaxSdkCallbacks.MRec.OnAdLoadedEvent += OnMRecAdLoadedEvent;
        MaxSdkCallbacks.MRec.OnAdLoadFailedEvent += OnMRecAdLoadFailedEvent;
        MaxSdkCallbacks.MRec.OnAdClickedEvent += OnMRecAdClickedEvent;
        MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;
        MaxSdkCallbacks.MRec.OnAdExpandedEvent += OnMRecAdExpandedEvent;
        MaxSdkCallbacks.MRec.OnAdCollapsedEvent += OnMRecAdCollapsedEvent;

        //Invoke(nameof(LoadMREC), 0.5f);
        DOVirtual.DelayedCall(0.5f, LoadMREC).SetUpdate(true);
    }

    public void LoadMREC()
    {
        // MRECs are sized to 300x250 on phones and tablets
        MaxSdk.CreateMRec(MrecAdUnitId, _defaultMRECPosition);
    }

    public void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { _isMrecLoaded = true; }

    public void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error) { }

    public void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    public void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    public void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    public void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    public void ShowMRec(Vector2 pixelPosition)
    {
        var density = MaxSdkUtils.GetScreenDensity();
        var dpPos = pixelPosition / density;
        //Vector2 dpPos = position / (Screen.dpi / 160f);
        MaxSdk.UpdateMRecPosition(MrecAdUnitId, dpPos.x, dpPos.y);
        ShowMRec();
    }

    public Vector2 GetMRecPixelSize()
    {
        var density = MaxSdkUtils.GetScreenDensity();
        Vector2 mrecSize = new Vector2(300, 250);
        //mrecSize = mrecSize * (Screen.dpi / 160f);
        mrecSize *= density;
        return mrecSize;
    }

    public void ShowMRec(MaxSdkBase.AdViewPosition viewPosition)
    {
        MaxSdk.UpdateMRecPosition(MrecAdUnitId, viewPosition);
        ShowMRec();
    }

    public void ShowMRecAboveBanner()
    {
#if UNITY_EDITOR
        Debug.Log("Show MRec Above banner");
#endif
        Vector2 screenSafeSize = new(Screen.safeArea.width, Screen.safeArea.height);
        Vector2 mrecScreenSize = GetMRecPixelSize();
        Vector2 mrecPosition = new((screenSafeSize.x - mrecScreenSize.x) * 0.5f, (screenSafeSize.y - mrecScreenSize.y) * 0.85f);
        ShowMRec(mrecPosition);
    }

    public void ShowMRec()
    {
        if (IsRemoveAds() == false && (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.mrec_ad_on_off))
        {
#if UNITY_EDITOR
            Debug.Log("Show MRec");
#endif

            IsMrecShow = true;
            OnShowMREC?.Invoke();
            MaxSdk.ShowMRec(MrecAdUnitId);
        }
    }

    public void HideMRec()
    {
#if UNITY_EDITOR
        Debug.Log("Hide MRec");
#endif

        MaxSdk.HideMRec(MrecAdUnitId);
        IsMrecShow = false;
    }
    #endregion

    #region Others
    public void ShowMessage(string msg)
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        AndroidJavaObject @static = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject androidJavaObject = new AndroidJavaClass("android.widget.Toast");
        androidJavaObject.CallStatic<AndroidJavaObject>("makeText", new object[]
        {
                @static,
                msg,
                androidJavaObject.GetStatic<int>("LENGTH_SHORT")
        }).Call("show", Array.Empty<object>());
#endif
    }

    public bool CheckInternet()
    {
        return Application.internetReachability == NetworkReachability.NotReachable ? false : true;
    }

    public bool IsRemoveAds()
    {
#if ENABLE_REMOVE_ADS
        return true;
#else
        return PlayerPrefs.GetInt("IsRemoveAds", 0) == 1;
#endif
    }

    public void RemoveAds()
    {
        NativeBannerController.Instance.HideNativeBanner();
        PlayerPrefs.SetInt("IsRemoveAds", 1);
    }

    public void ResetRemoveAds()
    {
        PlayerPrefs.SetInt("IsRemoveAds", 0);
    }
    #endregion

    [Flags]
    public enum TypeAds
    {
        Banner = 1 << 0,
        Inters = 1 << 1,
        Rewarded = 1 << 2,
        Mrec = 1 << 3,
        AOA = 1 << 4,
    }
}

