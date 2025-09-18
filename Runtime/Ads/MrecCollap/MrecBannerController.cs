using GoogleMobileAds.Api;
using HG.Rate;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MaxSdkBase;

public class MrecBannerController : MonoBehaviour
{
    [SerializeField] private GameObject _mrecBannerZone;
    [SerializeField] private RectTransform _mrecBannerRect;
    [SerializeField] private RectTransform _canvasRect;

    private bool _isShow = false;
    public static MrecBannerController I;

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        float mrecSize = AdsManager.Instance.GetMRecPixelSize().y;
        float screenSize = Screen.height;
        float canvasSize = _canvasRect.sizeDelta.y;
        float sizeZone = mrecSize * canvasSize / screenSize + 150f;

        _mrecBannerRect.sizeDelta = new Vector2(_mrecBannerRect.sizeDelta.x, sizeZone);
        _mrecBannerRect.anchoredPosition = new Vector2(_mrecBannerRect.anchoredPosition.x, (sizeZone) / 2f);
    }

    private void OnEnable()
    {
        AdsManager.OnShowMREC += AdsManager_OnShowMREC;
    }

    private void OnDisable()
    {
        AdsManager.OnShowMREC -= AdsManager_OnShowMREC;
    }

    private void AdsManager_OnShowMREC()
    {
        if (_isShow)
        {
            //CloseMrecBanner();
            //HideBanner();
        }
    }

    public void ShowBanner()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;
        _isShow = true;
        ShowBannerCustomRefresh();
    }

    public void ShowSmallBanner()
    {
        if (AdsManager.Instance.IsRemoveAds()) return;
        _isShow = true;
        NativeBannerController.Instance.ShowNativeBanner();
    }

    public void HideBanner()
    {
        _isShow = false;
        StopAllCoroutines();

        HideMREC();
        _mrecBannerZone.SetActive(false);

        NativeBannerController.Instance.HideNativeBanner();
    }

    public void CloseMrecBanner()
    {
        HideMREC();
        _mrecBannerZone.SetActive(false);

        NativeBannerController.Instance.ShowNativeBanner();
    }

    private void ShowBannerCustomRefresh()
    {
        StopAllCoroutines();
        bool isEnable = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.mrec_banner_on_off_user_organic);
        if (AttributionManager.IsUserOrganic == false)
            isEnable = (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.mrec_banner_on_off_user_paid);
        if (isEnable)
        {
            StartCoroutine(IE_ShowBannerCustomRefresh());
        }
        else
        {
            NativeBannerController.Instance.ShowNativeBanner();
        }
    }

    private IEnumerator IE_ShowBannerCustomRefresh()
    {
        while (true)
        {
            if (AdsManager.Instance.IsMrecShow == false)
            {
                if (AdsManager.Instance.IsMrecLoaded)
                {
                    ShowMREC();
                    _mrecBannerZone.SetActive(true);

                    NativeBannerController.Instance.HideNativeBanner();
                    StartCoroutine(IE_DelayShowSmallBanner());
                }
                else
                {
                    AdsManager.Instance.LoadMREC();
                    CloseMrecBanner();
                }
            }
            long timeWait = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_refresh_mrec_banner);
            yield return new WaitForSecondsRealtime(timeWait);
        }
    }

    private IEnumerator IE_DelayShowSmallBanner()
    {
        long timeWait = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_auto_close_mrec_banner);
        yield return new WaitForSecondsRealtime(timeWait);
        CloseMrecBanner();
    }

    private void ShowMREC()
    {
        MaxSdk.UpdateMRecPosition(AdsManager.Instance.MrecAdUnitId, AdViewPosition.BottomCenter);
        MaxSdk.ShowMRec(AdsManager.Instance.MrecAdUnitId);
    }

    private void HideMREC()
    {
        MaxSdk.HideMRec(AdsManager.Instance.MrecAdUnitId);
    }
}
