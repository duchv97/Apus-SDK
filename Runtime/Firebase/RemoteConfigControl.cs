using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;

public class RemoteConfigControl : MonoBehaviour
{
    public static RemoteConfigControl Instance { get; private set; }
    public static event Action OnFetchDone;
    public static bool IsFetchDone;

    [SerializeField] private RemoteVariableInfoSO _remoteVariableInfoSO;

    private readonly Dictionary<string, RemoteVariableInfo> _remoteVariables = new();

    private void Awake()
    {
        Instance = this;

        foreach (var variable in _remoteVariableInfoSO.RemoteVariableInfos)
        {
            variable.LoadData();
            _remoteVariables[variable.Name] = variable;
        }
    }

    private void OnEnable() => FirebaseInitialize.OnInitDone += InitializeRemoteConfig;
    private void OnDisable() => FirebaseInitialize.OnInitDone -= InitializeRemoteConfig;

    public object GetValue(EnumRemoteVariable typeRemoteVariable) =>
        _remoteVariables.TryGetValue(typeRemoteVariable.ToString(), out var variable) ? variable.GetValue() : null;

    private void InitializeRemoteConfig()
    {
        Dictionary<string, object> defaults = new();
        foreach (var variable in _remoteVariableInfoSO.RemoteVariableInfos)
        {
            switch (variable.TypeVariable)
            {
                case RemoteVariableInfo.EnumVariable.BooleanValue:
                    defaults[variable.Name] = variable.DefaultValueBoolean;
                    break;
                case RemoteVariableInfo.EnumVariable.DoubleValue:
                    defaults[variable.Name] = variable.DefaultValueDouble;
                    break;
                case RemoteVariableInfo.EnumVariable.LongValue:
                    defaults[variable.Name] = variable.DefaultValueLong;
                    break;
                case RemoteVariableInfo.EnumVariable.StringValue:
                    defaults[variable.Name] = variable.DefaultValueString;
                    break;
            }
        }

        Debug.Log("RemoteConfig configured and ready!");
        FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
        FetchDataAsync();
    }

    private void FetchDataAsync()
    {
        Debug.Log("Fetching remote config data...");
        FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero)
            .ContinueWithOnMainThread(FetchComplete);
    }

    private void FetchComplete(Task fetchTask)
    {
        if (fetchTask.IsCanceled)
        {
            Debug.LogWarning("Fetch operation was canceled.");
            return;
        }
        if (fetchTask.IsFaulted)
        {
            Debug.LogError("Fetch operation encountered an error.");
            return;
        }
        if (!fetchTask.IsCompleted)
        {
            Debug.LogWarning("Fetch operation is still pending.");
            return;
        }

        Debug.Log("Fetch operation completed successfully!");

        var info = FirebaseRemoteConfig.DefaultInstance.Info;
        switch (info.LastFetchStatus)
        {
            case LastFetchStatus.Success:
                FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                Debug.Log($"Remote data loaded successfully (Last fetch: {info.FetchTime})");
                RefreshProperties();
                break;
            case LastFetchStatus.Failure:
                Debug.LogError(info.LastFetchFailureReason == FetchFailureReason.Throttled
                    ? $"Fetch throttled until {info.ThrottledEndTime}"
                    : "Fetch failed for unknown reasons.");
                break;
            case LastFetchStatus.Pending:
                Debug.LogWarning("Latest Fetch call is still pending.");
                break;
        }
    }

    private void RefreshProperties()
    {
        foreach (var variable in _remoteVariableInfoSO.RemoteVariableInfos)
        {
            ConfigValue configValue = FirebaseRemoteConfig.DefaultInstance.GetValue(variable.Name);
            variable.SetValue(configValue);
            variable.SaveData();
        }

        IsFetchDone = true;
        OnFetchDone?.Invoke();
        Debug.Log("RemoteConfig properties refreshed successfully.");
    }
}
