using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelRating : MonoBehaviour
{
    [SerializeField] private bool _isForceRateWhenSelect5Star = true;
    [Header("Common")]
    [SerializeField] private GameObject _step1;
    [SerializeField] private GameObject _step2;

    [Header("Step 1")]
    [SerializeField] private Button[] _btnArray;
    [SerializeField] private Sprite _goldStar;
    [SerializeField] private Sprite _silverStar;
    [SerializeField] private Button _btnRate;
    [SerializeField] private Button _btnLater;
    [SerializeField] private Button _btnNever;

    [Header("Step 2")]
    [SerializeField] public Button _btnNoThanks;
    [SerializeField] public Button _btnGiveFeedback;

    private int _rateCount;

    public static bool IsCallShowRate
    {
        get { return PlayerPrefs.GetInt("PanelRating_IsCallShowRate", 0) == 1; }
        set { PlayerPrefs.SetInt("PanelRating_IsCallShowRate", value ? 1 : 0); }
    }

    public bool IsRated
    {
        get { return PlayerPrefs.GetInt("PanelRating_IsRated", 0) == 1; }
        set { PlayerPrefs.SetInt("PanelRating_IsRated", value ? 1 : 0); }
    }

    private void Awake()
    {
        for (int i = 0; i < _btnArray.Length; i++)
        {
            int starIndex = i;
            var btn = _btnArray[starIndex];

            btn.onClick.AddListener(() =>
            {
                OnChooseStar(starIndex);
            });
        }

        _btnRate.onClick.AddListener(() =>
        {
            RateForUs(_rateCount);
            IsRated = true;
        });

        _btnLater.onClick.AddListener(() =>
        {
            Hide();
        });

        _btnNever.onClick.AddListener(() =>
        {
            IsRated = true;
            Hide();
        });

        _btnNoThanks.onClick.AddListener(() =>
        {
            IsRated = true;
            Hide();
        });

        _btnGiveFeedback.onClick.AddListener(() =>
        {
            IsRated = true;
            Hide();
        });
    }

    public bool ShowIfSatisfiedCondition(int level, bool isShowImmediate = true)
    {
        if (level >= (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.level_show_rate))
            IsCallShowRate = true;
        else
            return false;
        if (IsRated == false)
        {
            if (isShowImmediate)
                Show();
            return true;
        }
        return false;
    }

    public bool ShowIfSatisfiedCondition(bool isShowImmediate = true)
    {
        IsCallShowRate = true;
        if (IsRated == false)
        {
            if (isShowImmediate)
                Show();
            return true;
        }
        return false;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void OnShow()
    {
        for (int i = 0; i < _btnArray.Length; i++)
        {
            _btnArray[i].image.sprite = _silverStar;
        }
        _btnRate.interactable = false;

        _step1.SetActive(true);
        _step2.SetActive(false);
    }

    private void OnChooseStar(int star)
    {
        _rateCount = star;
        StartCoroutine(I_Choose());
    }

    private IEnumerator I_Choose()
    {
        for (int i = 0; i < _btnArray.Length; i++)
        {
            _btnArray[i].image.sprite = _silverStar;
        }
        for (int i = 0; i < _rateCount + 1; i++)
        {
            _btnArray[i].image.sprite = _goldStar;
            yield return new WaitForSeconds(0.1f);
        }
        _btnRate.interactable = true;

        //force rate when 5 star
        if (_isForceRateWhenSelect5Star && _rateCount == 4)
        {
            RateForUs(_rateCount);
            IsRated = true;
        }
    }

    public void RateForUs(int rateCount)
    {
        _rateCount = rateCount;
        StartCoroutine(I_Rate(rateCount));
    }

    private IEnumerator I_Rate(int rateCount)
    {
        float delay = rateCount * 0.1f + 0.5f;
        if (rateCount >= 4)
        {
#if UNITY_ANDROID
            InAppReview.Instance.StartRequestReview();
#elif UNITY_IPHONE
            UnityEngine.iOS.Device.RequestStoreReview();
#endif
            yield return new WaitForSeconds(delay);
            Hide();
        }
        else
        {
            _step1.SetActive(false);
            _step2.SetActive(true);
        }
    }
}
