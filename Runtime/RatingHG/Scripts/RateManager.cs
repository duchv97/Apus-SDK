using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

#if UNITY_ANDROID
using Google.Play.Review;
#elif UNITY_IOS || UNITY_IPHONE
using UnityEngine.iOS;
#endif

namespace HG.Rate
{
    /// <summary>
    /// Them show ADS tai ham HidePopup()
    /// </summary>
    public class RateManager : MonoBehaviour
    {
        public static bool IsCallShowRate
        {
            get { return PlayerPrefs.GetInt("RateManager_IsCallShowRate", 0) != 0; }
            set { PlayerPrefs.SetInt("RateManager_IsCallShowRate", value ? 1 : 0); }
        }

        public static bool IsRated
        {
            get { return PlayerPrefs.GetInt("RateManager_IsRated", 0) != 0; }
            set { PlayerPrefs.SetInt("RateManager_IsRated", value ? 1 : 0); }
        }

        public static RateManager I;
        public static bool IsHideBannerByRate;

        private void Awake()
        {
            I = this;
        }

        #region SerializeField
        [SerializeField] List<GameObject> starsOn;
        [SerializeField] RectTransform starRow;
        [SerializeField] GameObject feedbackObj, titleObject;
        [SerializeField] RectTransform container;
        [SerializeField] Transform topPoint;
        [SerializeField] Canvas canvas;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] Button btnBackdrop;
        [SerializeField] GameObject objSubmitGray, objSubmitGreen;
        #endregion
        #region variables
        float effectTime = 0.2f;
        Sequence sequence1, sequence2;
        bool showFB = false;
        float panelHeight = 852;
        #endregion
        #region Popup Func

        public bool IsShow { get; private set; }
        public static event Action OnHide;

        //level start from 1
        public bool ShowPopupWithLevelCondition(int curLevel, int intervalLevelShow)
        {
            long levelShowRate = (long)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.level_show_rate);
            if (IsRated == false && (curLevel - levelShowRate) % intervalLevelShow == 0)
            {
                ShowPopup();
                return true;
            }
            return false;
        }

        public void CheckShowRate()
        {
            if (IsRated || IsCallShowRate || IsShow) return;
            IsHideBannerByRate = true;
            ShowPopup();
        }

        public bool CanShowRating() => (bool)RemoteConfigControl.Instance.GetValue(EnumRemoteVariable.rating_filter_on_off);

        [Button]
        public void ShowPopup()
        {
            AdsManager.Instance.IsCanShowInterAds = true;

            if (!CanShowRating())
            {
                return;
            }

            IsShow = true;

            objSubmitGray.SetActive(true);
            objSubmitGreen.SetActive(false);
            for (int i = 0; i < starsOn.Count; i++) starsOn[i].SetActive(false);
            showFB = false;

            canvas.enabled = true;
            container.anchoredPosition = new Vector2(0, -panelHeight);
            sequence1?.Kill();
            sequence1 = DOTween.Sequence();
            sequence1.Append(DOVirtual.Float(0, 1, effectTime, (val) => { canvasGroup.alpha = val; }));
            //sequence1.Join(container.DOAnchorPosY(0, effectTime).SetEase(Ease.Linear));
            sequence1.OnComplete(() => { btnBackdrop.onClick.AddListener(HidePopup); });
            sequence1.SetUpdate(true);
        }
        public void HidePopup()
        {
            IsCallShowRate = true;
            IsShow = false;

            btnBackdrop.onClick.RemoveAllListeners();
            sequence1?.Kill();
            sequence1 = DOTween.Sequence();
            //sequence1.Append(container.DOAnchorPosY(-panelHeight, effectTime).SetEase(Ease.Linear));
            sequence1.Join(DOVirtual.Float(1, 0, effectTime, (val) => { canvasGroup.alpha = val; }));
            sequence1.OnComplete(() =>
            {
                canvas.enabled = false;
                AdsManager.Instance.ShowInterstitial(
                    () =>
                    {
                        OnHide?.Invoke();
                    }, "Rate_Close");
            });
            sequence1.SetUpdate(true);
        }
        #endregion
        #region Others
        public void OnStarClick(int value)
        {
            objSubmitGray.SetActive(false);
            objSubmitGreen.SetActive(true);
            for (int i = 0; i < starsOn.Count; i++)
            {
                starsOn[i].SetActive(value >= i);
            }
            if (value < 4)
            {
                ShowFeedback();
                return;
            }
            RateAndReview();
            IsRated = true;
        }


        void ShowFeedback()
        {
            if (showFB) return;
            showFB = true;
            feedbackObj.transform.localScale = Vector3.zero;
            titleObject.SetActive(false);
            sequence2?.Kill();
            sequence2 = DOTween.Sequence();
            sequence2.Append(starRow.DOMove(topPoint.position, effectTime).SetEase(Ease.Linear));
            sequence2.Join(feedbackObj.transform.DOScale(1, effectTime).SetEase(Ease.Linear));
            sequence2.SetUpdate(true);
        }

        private void OnDestroy()
        {
            sequence1?.Kill();
            sequence2?.Kill();
        }

#if UNITY_ANDROID

        private ReviewManager _reviewManager;
        private PlayReviewInfo _playReviewInfo;
        private Coroutine _coroutine;

        protected void Start()
        {
            _coroutine = StartCoroutine(InitReview());
        }

        private IEnumerator InitReview(bool force = false)
        {
            if (_reviewManager == null) _reviewManager = new ReviewManager();

            var requestFlowOperation = _reviewManager.RequestReviewFlow();
            yield return requestFlowOperation;
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
            {
                if (force) DirectlyOpen();
                yield break;
            }


            _playReviewInfo = requestFlowOperation.GetResult();
        }

        public IEnumerator LaunchReview()
        {
            if (_playReviewInfo == null)
            {
                if (_coroutine != null) StopCoroutine(_coroutine);
                yield return StartCoroutine(InitReview(true));
            }

            var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
            yield return launchFlowOperation;
            _playReviewInfo = null;

            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
            {
                DirectlyOpen();
                yield break;
            }
        }
#endif

        public void RateAndReview()
        {
#if UNITY_IOS
            Device.RequestStoreReview();
#elif UNITY_ANDROID
            StartCoroutine(LaunchReview());
#endif
            HidePopup();
        }

        private void DirectlyOpen() { Application.OpenURL($"https://play.google.com/store/apps/details?id={Application.identifier}"); }
        #endregion
    }
}
