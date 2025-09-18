using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdmobBannerLoader : MonoBehaviour
{
    private string _bannerAdUnitId = "ca-app-pub-2913496970595341/3717207558";

    public Action OnLoadFail;
    public Action OnLoadSuccess;
    public Action OnContentOpen;
    public Action OnContentClosed;

    private BannerView _bannerView;

    public void SetID(string id)
    {
        _bannerAdUnitId = id;
    }

    public bool IsCollapsible()
    {
        return _bannerView.IsCollapsible();
    }

    public void CreateBannerView(AdSize adSize, AdPosition adPosition)
    {
        Debug.Log("Creating banner view : " + _bannerAdUnitId);

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyBanner();
        }

        // Create a 320x50 banner at top of the screen.
        _bannerView = new BannerView(_bannerAdUnitId, adSize, adPosition);

        // Listen to events the banner may raise.
        ListenToAdEvents();

        Debug.Log("Banner view created.");
    }

    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadBanner(AdSize adSize, AdPosition adPosition, bool isCollapsible)
    {
        // Create an instance of a banner view first.
        if (_bannerView == null)
        {
            CreateBannerView(adSize, adPosition);
        }

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        if (isCollapsible)
            adRequest.Extras.Add("collapsible", "bottom");

        // Send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(adRequest);
        HideBanner();
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowBanner()
    {
        if (_bannerView != null)
        {
            Debug.Log("Showing banner view.");
            _bannerView.Show();
        }
    }

    /// <summary>
    /// Hides the ad.
    /// </summary>
    public void HideBanner()
    {
        if (_bannerView != null)
        {
            Debug.Log("Hiding banner view.");
            _bannerView.Hide();
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// When you are finished with a BannerView, make sure to call
    /// the Destroy() method before dropping your reference to it.
    /// </summary>
    public void DestroyBanner()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
        }
    }

    /// <summary>
    /// Listen to events the banner may raise.
    /// </summary>
    private void ListenToAdEvents()
    {
        // Raised when an ad is loaded into the banner view.
        _bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + _bannerView.GetResponseInfo());
            OnLoadSuccess?.Invoke();
        };
        // Raised when an ad fails to load into the banner view.
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view failed to load an ad with error : " + error);
            OnLoadFail?.Invoke();
        };
        // Raised when the ad is estimated to have earned money.
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));

            AdmobManager.Instance.LogAdRevenueAdmob(adValue, _bannerView.GetResponseInfo());
        };
        // Raised when an impression is recorded for an ad.
        _bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        _bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
            OnContentOpen?.Invoke();
        };
        // Raised when the ad closed full screen content.
        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");
            OnContentClosed?.Invoke();
        };
    }
}