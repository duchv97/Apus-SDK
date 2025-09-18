using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;
using System;
using DG.Tweening;

public class NativeAdsBillboard : MonoBehaviour
{
    [SerializeField] private Vector3 _originalPosition;
    [SerializeField] private MeshRenderer _mesh;
    [SerializeField] private NativeAdsBillboardClickChecker[] _nativeAdsBillboardClickCheckers;

    private bool nativeAdLoaded;
    private NativeAd nativeAd;
    private bool _isEnableClick = true;

    public static NativeAdsBillboard I;

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        if (AdsManager.Instance.IsRemoveAds() == false && (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.native_ad_on_off))
            StartCoroutine(IE_RequestLoop());
    }

    private IEnumerator IE_RequestLoop()
    {
        while (true)
        {
            if (_isEnableClick)
                RequestNativeAd();
            long time = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.time_refresh_native_ads);
            yield return new WaitForSeconds(time);
        }
    }

    private void RequestNativeAd()
    {
        nativeAdLoaded = false;
        string id = (string)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.NATIVE_AD_UNIT_ID);
        //id = "ca-app-pub-3940256099942544/2247696110";
        AdLoader adLoader = new AdLoader.Builder(id)
            .ForNativeAd()
            .Build();
        adLoader.OnNativeAdLoaded += this.HandleNativeAdLoaded;
        adLoader.OnNativeAdClicked += AdLoader_OnNativeAdClicked;
        adLoader.LoadAd(new AdRequest());

        Debug.Log("RequestNativeAd");
    }

    private void AdLoader_OnNativeAdClicked(object sender, EventArgs e)
    {
    }
    private void HandleNativeAdLoaded(object sender, NativeAdEventArgs e)
    {
        this.nativeAd = e.nativeAd;
        this.nativeAdLoaded = true;
    }

    private void Update()
    {
        if (this.nativeAdLoaded)
        {
            nativeAdLoaded = false;
            _mesh.material.SetTexture("_BaseMap", nativeAd.GetImageTextures()[0]);
            nativeAd.RegisterIconImageGameObject(_mesh.gameObject);
        }
    }

    public void CheckState()
    {
        if (IsCanClick())
            EnableClick();
        else
            DisableClick();
    }

    private bool IsCanClick()
    {
        for (int i = 0; i < _nativeAdsBillboardClickCheckers.Length; i++)
        {
            if (_nativeAdsBillboardClickCheckers[i].IsShow)
                return false;
        }
        return true;
    }

    private void EnableClick()
    {
        _isEnableClick = true;
        DOVirtual.DelayedCall(1f, () =>
        {
            if (_isEnableClick)
            {
                transform.position = _originalPosition;
            }
        });
    }

    private void DisableClick()
    {
        _isEnableClick = false;
        transform.position = Vector3.forward * 1000;
    }
}
