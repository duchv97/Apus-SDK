using AppsFlyerSDK;
using GoogleMobileAds.Api;
using System.Collections;
using UnityEngine;
using static GoogleMobileAds.Api.AdValue;

public class NativeBannerController : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField] private NativeBannerAdView nativeBannerView;
    [SerializeField] private bool isPortrait = true;
    [SerializeField] private float adSizeWidth = 1f;
    #endregion

    #region Singleton & Properties
    public static NativeBannerController Instance { get; private set; }

    public NativeBannerAdView NativeBannerView => nativeBannerView;

    public AndroidJavaObject Activity { get; private set; }

    public AndroidJavaObject AdManager { get; private set; }

    private bool IsAndroidPlatform => Application.platform == RuntimePlatform.Android;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeAndroidComponents();
    }

    private void Start()
    {
        UpdateNativeBannerSettings();
    }

    private void OnEnable()
    {
        RemoteConfigControl.OnFetchDone += RemoteConfigControl_FetchDone;
        AttributionManager.OnAttributionDataProcessed += AttributionManager_OnAttributionDataProcessed;
    }

    private void OnDisable()
    {
        RemoteConfigControl.OnFetchDone -= RemoteConfigControl_FetchDone;
        AttributionManager.OnAttributionDataProcessed -= AttributionManager_OnAttributionDataProcessed;
    }
    #endregion

    #region Public API

    public void ShowNativeBanner()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;

        nativeBannerView.gameObject.SetActive(true);
        if (IsAndroidPlatform)
        {
            AdManager.Call("showNativeAd", Activity);
        }
    }

    public void HideNativeBanner()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;

        nativeBannerView.gameObject.SetActive(false);
        if (IsAndroidPlatform)
        {
            AdManager.Call("hideNativeAd", Activity);
        }
    }
    #endregion

    #region Event Handlers
    public void OnAdEvent(string json)
    {
        Debug.Log($"[NativeBannerController] OnAdEvent: {json}");
        try
        {
            var eventData = JsonUtility.FromJson<AdEventData>(json);
            HandleAdEvent(eventData);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NativeBannerController] Failed to parse ad event JSON: {ex.Message}");
        }
    }

    private void HandleAdEvent(AdEventData eventData)
    {
        switch (eventData.eventName)
        {
            case "AdLoaded":
                AnalyticsManager.Instance.LogEvent("ad_banner_loaded");
                break;
            case "AdClicked":
                AnalyticsManager.Instance.LogEvent("ad_banner_clicked");
                break;
            case "PaidEvent":
                HandlePaidEvent(eventData);
                break;
            case "AdImpression":
                AnalyticsManager.Instance.LogEvent("ad_banner_impression");
                break;
                // Thêm các case khác nếu cần...
        }
    }

    private void HandlePaidEvent(AdEventData eventData)
    {
        var adValue = new AdValue
        {
            Value = eventData.valueMicros,
            CurrencyCode = eventData.currencyCode,
            Precision = (PrecisionType)eventData.precisionType
        };
        AdmobManager.Instance.LogAdRevenueAdmob(adValue, null);
    }
    #endregion

    #region Private Helpers
    private void InitializeAndroidComponents()
    {
        if (!IsAndroidPlatform) return;

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            Activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }
        var overlayClass = new AndroidJavaClass("com.example.admoblibrary.NativeAdOverlay");
        AdManager = overlayClass.CallStatic<AndroidJavaObject>("getInstance");
        AdManager.Call("setupLayout", Activity, isPortrait, adSizeWidth);
    }

    private void AttributionManager_OnAttributionDataProcessed(string mediaSource, bool isUserOrganic)
    {
        if (!IsAndroidPlatform) return;
        AdManager.Call("setAttributionData", Activity, mediaSource, isUserOrganic);
    }


    private void RemoteConfigControl_FetchDone()
    {
        UpdateNativeBannerSettings();
    }

    private void UpdateNativeBannerSettings()
    {
        nativeBannerView.Init(
            (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.BANNER_NATIVE_AD_UNIT_ID),
            (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.banner_native_refresh_time)
        );

        if (!IsAndroidPlatform) return;

        long defaultCappingTime = 300;
        bool bannerOnOff = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.banner_native_on_off);
        string timeCappingJson = (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.banner_native_time_capping_json);
        string bannerSizesJson = (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.banner_native_size_json);

        AdManager.Call("initialize",
            Activity,
            bannerOnOff,
            defaultCappingTime,
            timeCappingJson,
            bannerSizesJson
        );
    }
    #endregion
}


[System.Serializable]
public class AdEventData
{
    public string eventName;
    public long valueMicros;
    public string currencyCode;
    public int precisionType;
    public string errorCode;
    public string message;
}