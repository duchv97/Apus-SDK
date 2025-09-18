using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BannerAdsLoader : MonoBehaviour
{
    private void OnEnable()
    {
#if !UNITY_EDITOR
        if (AdsManager.Instance)
            AdsManager.Instance.ShowBanner();
#endif
    }

    private void OnDisable()
    {
#if !UNITY_EDITOR
        if (AdsManager.Instance)
            AdsManager.Instance.HideBanner();
#endif
    }
}
