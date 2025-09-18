using System;
using System.Collections;
using System.Collections.Generic;
using AppsFlyerSDK;
using Newtonsoft.Json;
using UnityEngine;

public class AttributionManager : MonoBehaviour
{
    [SerializeField] private bool _isDebug = false;

    #region Static Properties using Easy Save 3
    public static bool IsCheckedUserOrganic
    {
        get { return PlayerPrefs.GetInt("AttributionManager_IsCheckedUserOrganic", 0) == 1; }
        private set { PlayerPrefs.SetInt("AttributionManager_IsCheckedUserOrganic", value ? 1 : 0); }
    }

    public static bool IsUserOrganic
    {
        get { return PlayerPrefs.GetInt("AttributionManager_IsUserOrganic", 1) == 1; }
        private set { PlayerPrefs.SetInt("AttributionManager_IsUserOrganic", value ? 1 : 0); }
    }

    public static string MediaSource
    {
        get { return PlayerPrefs.GetString("AttributionManager_MediaSource", defaultValue: "organic"); }
        private set { PlayerPrefs.SetString("AttributionManager_MediaSource", value); }
    }
    #endregion

    #region Events
    public static event Action<string, bool> OnAttributionDataProcessed;
    #endregion

    #region Private Fields
    private List<string> rawNonOrganicSources = new List<string>();
    private bool isRemoteConfigReady = false;
    #endregion

    #region Unity Lifecycle Methods

    private void OnEnable()
    {
        RemoteConfigControl.OnFetchDone += HandleRemoteConfigFetchDone;
        AppsFlyerManager.OnConversionDataSuccess += AppsFlyerManager_OnConversionDataSuccess;
    }

    private void OnDisable()
    {
        RemoteConfigControl.OnFetchDone -= HandleRemoteConfigFetchDone;
        AppsFlyerManager.OnConversionDataSuccess -= AppsFlyerManager_OnConversionDataSuccess;
    }
    #endregion

    #region Private Helpers & Coroutines

    private void AppsFlyerManager_OnConversionDataSuccess(string obj)
    {
        ProcessConversionData(obj);
    }

    public void ProcessConversionData(string conversionData)
    {
        if (IsCheckedUserOrganic)
        {
            if (_isDebug)
                Debug.Log("[AttributionManager] User status already checked. Firing event with stored data.");

            OnAttributionDataProcessed?.Invoke(MediaSource, IsUserOrganic);

            return;
        }

        StartCoroutine(IE_ProcessConversionData(conversionData));
    }

    private void HandleRemoteConfigFetchDone()
    {
        string nonOrganicSourcesJson = (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.MEDIA_SOURCE_NON_ORGANIC);
        ParseNonOrganicSources(nonOrganicSourcesJson);
        isRemoteConfigReady = true;
    }

    private void ParseNonOrganicSources(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            rawNonOrganicSources = new List<string>();
            return;
        }

        try
        {
            rawNonOrganicSources = JsonConvert.DeserializeObject<List<string>>(json);
            if (_isDebug)
                Debug.Log($"[AttributionManager] Parsed non-organic sources using Newtonsoft.Json. Total: {rawNonOrganicSources.Count}");
        }
        catch (JsonException ex)
        {
            Debug.LogError($"[AttributionManager] Failed to parse non-organic sources JSON with Newtonsoft.Json. Error: {ex.Message}");
            rawNonOrganicSources = new List<string>();
        }
    }

    private IEnumerator IE_ProcessConversionData(string conversionData)
    {
        yield return new WaitUntil(() => isRemoteConfigReady);

        var conversionDataDictionary = AppsFlyer.CallbackStringToDictionary(conversionData);

        string mediaSource = "organic";
        if (conversionDataDictionary.TryGetValue("media_source", out object mediaSourceObj))
        {
            mediaSource = mediaSourceObj.ToString();
        }

        bool isNonOrganic = rawNonOrganicSources.Contains(mediaSource);

        MediaSource = mediaSource;
        IsUserOrganic = !isNonOrganic;
        IsCheckedUserOrganic = true;

        if (_isDebug)
            Debug.Log($"[AttributionManager] Attribution data processed and saved. MediaSource: {MediaSource}, IsOrganic: {IsUserOrganic}");

        OnAttributionDataProcessed?.Invoke(MediaSource, IsUserOrganic);
    }
    #endregion
}