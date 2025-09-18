using System;
using System.Collections.Generic;
using Firebase.Analytics;
//using GameAnalyticsSDK;
using UnityEngine;
//using UnityEngine.Purchasing;

public static class AnalyticsEvent
{
    public const string LEVEL_START = "level_start";
    public const string LEVEL_COMPLETE = "level_complete";
    public const string LEVEL_FAIL = "level_fail";
    public const string EARN_VIRTUAL_CURRENCY = "earn_virtual_currency";
    public const string SPEND_VIRTUAL_CURRENCY = "spend_virtual_currency";
}

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable() => FirebaseInitialize.OnInitDone += OnFirebaseInitialized;
    private void OnDisable() => FirebaseInitialize.OnInitDone -= OnFirebaseInitialized;

    private void OnFirebaseInitialized()
    {
        
        DateTime now = DateTime.Now;
        string nowJson = JsonUtility.ToJson(now);
        DateTime lastTimePlay = JsonUtility.FromJson<DateTime>(PlayerPrefs.GetString("lastTimePlay", nowJson));
        DateTime firstTimePlay = JsonUtility.FromJson<DateTime>(PlayerPrefs.GetString("firstTimePlay", nowJson));

        PlayerPrefs.SetString("lastTimePlay", nowJson);

        int daysPlayed = PlayerPrefs.GetInt("days_played", 1);
        if (lastTimePlay.Day != now.Day)
        {
            daysPlayed++;
            PlayerPrefs.SetInt("days_played", daysPlayed);
        }

        int retentionType = (firstTimePlay.Day != now.Day) ? Mathf.FloorToInt((float)(now - firstTimePlay).TotalDays) : 0;
        int highLevel = PlayerPrefs.GetInt("high_level", 0) + 1;

        SetUserProperty("level", highLevel.ToString());
        SetUserProperty("retention_type", retentionType.ToString());
        SetUserProperty("days_played", daysPlayed.ToString());
    }

    public Parameter ToParameter(KeyValuePair<string, object> kvp)
    {
        var type = kvp.Value.GetType();

        if (type == typeof(double))
            return new Parameter(kvp.Key, (double)kvp.Value);
        else if (type == typeof(long))
            return new Parameter(kvp.Key, (long)kvp.Value);
        else
            return new Parameter(kvp.Key, (string)kvp.Value);
    }

    public void LogLevelStartEvent(string level, Dictionary<string, object> customFields = null)
    {
        try
        {
            if (customFields != null)
            {
                int index = 0;
                Parameter[] parameters = new Parameter[customFields.Count + 1];
                parameters[index] = new Parameter("level", level);
                foreach (var kvp in customFields)
                {
                    index++;
                    parameters[index] = ToParameter(kvp);
                }

                FirebaseAnalytics.LogEvent(AnalyticsEvent.LEVEL_START, parameters);
                //GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, level, customFields);
            }
            else
            {
                FirebaseAnalytics.LogEvent(AnalyticsEvent.LEVEL_START, "level", level);
                //GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, level);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void LogLevelCompleteEvent(string level, Dictionary<string, object> customFields = null)
    {
        try
        {
            if (customFields != null)
            {
                int index = 0;
                Parameter[] parameters = new Parameter[customFields.Count + 1];
                parameters[index] = new Parameter("level", level);
                foreach (var kvp in customFields)
                {
                    index++;
                    parameters[index] = ToParameter(kvp);
                }

                FirebaseAnalytics.LogEvent(AnalyticsEvent.LEVEL_COMPLETE, parameters);
                //GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, level, customFields);
            }
            else
            {
                FirebaseAnalytics.LogEvent(AnalyticsEvent.LEVEL_COMPLETE, "level", level);
                //GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, level);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void LogLevelFailEvent(string level, Dictionary<string, object> customFields = null)
    {
        try
        {
            if (customFields != null)
            {
                int index = 0;
                Parameter[] parameters = new Parameter[customFields.Count + 1];
                parameters[index] = new Parameter("level", level);
                foreach (var kvp in customFields)
                {
                    index++;
                    parameters[index] = ToParameter(kvp);
                }

                FirebaseAnalytics.LogEvent(AnalyticsEvent.LEVEL_FAIL, parameters);
                //GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, level, customFields);
            }
            else
            {
                FirebaseAnalytics.LogEvent(AnalyticsEvent.LEVEL_FAIL, "level", level);
                //GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, level);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void LogEvent(string eventName, Dictionary<string, object> customFields = null)
    {
        try
        {
            if (customFields != null)
            {
                int index = 0;
                Parameter[] parameters = new Parameter[customFields.Count];
                foreach (var kvp in customFields)
                {
                    parameters[index] = ToParameter(kvp);
                    index++;
                }

                FirebaseAnalytics.LogEvent(eventName, parameters);
                //GameAnalytics.NewDesignEvent(eventName, customFields, true);
            }
            else
            {
                FirebaseAnalytics.LogEvent(eventName);
                //GameAnalytics.NewDesignEvent(eventName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging event {eventName}: {e.Message}");
        }
    }

    public void LogVirtualCurrencyEvent(string eventName, string currencyName, string value, string additionalParam = null)
    {
        try
        {
            FirebaseAnalytics.LogEvent(eventName, new Parameter("virtualCurrencyName", currencyName), new Parameter("value", value),
             additionalParam == null ? null : new Parameter("source", additionalParam));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging event {eventName}: {e.Message}");
        }
    }

    public void LogAdsEvent(string eventName, string placement, string error = "")
    {
        try
        {
            FirebaseAnalytics.LogEvent(eventName, new Parameter("placement", placement));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error logging event {eventName}: {e.Message}");
        }
    }

    // public void LogIAPEvent(PurchaseEventArgs args)
    // {
    //     var product = args.purchasedProduct;
    //     int amount = decimal.ToInt32(product.metadata.localizedPrice * 100);
    //     string itemId = product.definition.id;

    //     GameAnalytics.NewBusinessEvent(product.metadata.isoCurrencyCode, amount, "", itemId, "");
    // }

    public void LogCheckpointEvent(string checkPoint) => LogEvent($"check_point_{checkPoint}");

    public void SetUserProperty(string name, string value)
    {
        Debug.Log($"Setting User Property: {name} - {value}");
        try
        {
            FirebaseAnalytics.SetUserProperty(name, value);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error setting user property {name}: {e.Message}");
        }
    }
}
