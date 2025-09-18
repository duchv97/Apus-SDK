// InAppUpdateManager.cs
using UnityEngine;
using UnityEngine.UI;
using System;

public class InAppUpdateManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Drag the GameObject containing the UI Popup here.")]
    [SerializeField] private GameObject updatePopup;

    [Tooltip("Drag the 'Update' Button UI element here.")]
    [SerializeField] private Button updateButton;

    [Header("Configs")]
    [Tooltip("The application's Apple App ID from the App Store.")]
    [SerializeField] private string APPLE_APP_ID = "";

    private void Awake()
    {
        // Hide popup and assign button listener
        if (updatePopup != null) updatePopup.SetActive(false);
        if (updateButton != null) updateButton.onClick.AddListener(OnUpdateButtonClicked);

        // 2. Subscribe to the event safely
        RemoteConfigControl.OnFetchDone += OnRemoteConfigFetched;

#if UNITY_IOS
        // Validate that the Apple App ID has been set in the Inspector.
        if (string.IsNullOrEmpty(APPLE_APP_ID) || APPLE_APP_ID == "YOUR_APPLE_APP_ID_HERE")
        {
            Debug.LogError("Apple App ID has not been set in the InAppUpdateManager's Inspector. The update button will not work correctly on iOS.");
        }
#endif
    }

    private void OnDestroy()
    {
        // Unsubscribe safely
        RemoteConfigControl.OnFetchDone -= OnRemoteConfigFetched;
    }

    // 3. Renamed the event handler for better clarity
    private void OnRemoteConfigFetched()
    {
        // Suggestion: The property name in RemoteConfigControl could be 'ForceUpdateGameInfo'
        CheckForUpdate((string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.force_update_game_info));
    }

    /// <summary>
    /// Checks for a mandatory update using a version name string (e.g., "1.0.1").
    /// </summary>
    /// <param name="remoteConfigJson">The JSON string from your remote config service.</param>
    public void CheckForUpdate(string remoteConfigJson)
    {
        if (string.IsNullOrEmpty(remoteConfigJson))
        {
            Debug.LogWarning("JSON string from Remote Config is null or empty.");
            return;
        }
        try
        {
            UpdateInfo updateInfo = JsonUtility.FromJson<UpdateInfo>(remoteConfigJson);

            if (updateInfo != null && updateInfo.isEnabled)
            {
                Version currentVersion = new Version(Application.version);
                Version minVersion = new Version(updateInfo.minVersion);

                Debug.Log($"Version Check: Required='{minVersion}', Current='{currentVersion}'");

                if (currentVersion < minVersion)
                {
                    ShowUpdatePopup();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing or comparing version: {ex.Message}");
        }
    }

    private void ShowUpdatePopup()
    {
        if (updatePopup != null)
        {
            updatePopup.SetActive(true);
        }
    }

    private void OnUpdateButtonClicked()
    {
#if UNITY_ANDROID
        Application.OpenURL("market://details?id=" + Application.identifier);
#elif UNITY_IOS
        Application.OpenURL("itms-apps://itunes.apple.com/app/id" + APPLE_APP_ID);
#else
        Debug.Log("Redirecting to the store page...");
#endif
    }
}

[Serializable]
public class UpdateInfo
{
    public bool isEnabled;
    public string minVersion;
}