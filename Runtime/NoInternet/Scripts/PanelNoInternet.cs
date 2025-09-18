using System;
using UnityEngine;
using UnityEngine.UI;

public class PanelNoInternet : MonoBehaviour
{
    [SerializeField] private GameObject _content;
    [SerializeField] private Button _btnOk;

    private bool _isShow;

    public bool IsShow { get => _isShow; }

    public void OnShow()
    {
        Time.timeScale = 0f;
        _isShow = true;
        _content.SetActive(true);

        //AdsManager.OnNativeAdsClickEnabled?.Invoke(false);
    }

    public void OnHide()
    {
        Time.timeScale = 1f;

        _isShow = false;
        _content.SetActive(false);

        AdsManager.Instance.TryToReInit();
        //AdsManager.OnNativeAdsClickEnabled?.Invoke(true);
    }

    void Update()
    {
        if (_isShow)
        {
            if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
                Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                OnHide();
            }
        }
        else
        {
            if (Application.internetReachability == NetworkReachability.NotReachable
                && (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.offline_play_on_off))
                OnShow();
        }

#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.X) && !_isShow)
        {
            OnShow();
            return;
        }
#endif
    }

    public void OnBtnOkClicked()
    {
        Debug.Log("Click Open setting internet");
        try
        {
#if UNITY_ANDROID
            using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject currentActivityObject = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (var intentObject = new AndroidJavaObject("android.content.Intent", "android.settings.WIFI_SETTINGS"))
                {
                    currentActivityObject.Call("startActivity", intentObject);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }
}