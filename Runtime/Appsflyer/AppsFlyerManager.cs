using System;
using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

public class AppsFlyerManager : MonoBehaviour, IAppsFlyerConversionData
{
    public static AppsFlyerManager Instance;

    public string devKey = "";
    [Header("Required for IOS")]
    public string appID = "";

    private bool _isInit;
    public static event Action<string> OnConversionDataSuccess;

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
#if UNITY_ANDROID
        AppsFlyer.initSDK(devKey, Application.identifier, this);
#elif UNITY_IOS
        AppsFlyer.initSDK(devKey, appID, this);
          if (appID.Equals(""))
            Debug.LogError("AppId is empty");
#endif
        AppsFlyer.startSDK();
    }

    public void onConversionDataSuccess(string conversionData)
    {
        if (_isInit) return;
        _isInit = true;

        AppsFlyer.AFLog("onConversionDataSuccess", conversionData);

        OnConversionDataSuccess?.Invoke(conversionData);

        // add deferred deeplink logic here
    }
    public void onConversionDataFail(string error)
    {
        AppsFlyer.AFLog("onConversionDataFail", error);
    }

    public void onAppOpenAttribution(string attributionData)
    {
        AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        Dictionary<string, object> attributionDataDictionary = AppsFlyer.CallbackStringToDictionary(attributionData);
        // add direct deeplink logic here
    }

    public void onAppOpenAttributionFailure(string error)
    {
        AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
    }
}
