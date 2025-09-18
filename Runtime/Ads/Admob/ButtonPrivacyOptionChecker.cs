using Apus.GoogleMobileAds;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Apus.GoogleMobileAds
{
    public class ButtonPrivacyOptionChecker : MonoBehaviour
    {
        [SerializeReference] private ButtonPrivacyOption _buttonPrivacyOption;
        [SerializeReference] private UnityEvent _onButtonStatusOn = new();
        [SerializeReference] private UnityEvent _onButtonStatusOff = new();

        private void Awake()
        {
            UpdateStatusButton();
        }

        private void OnEnable()
        {
            GoogleMobileAdsConsentController.OnChangePrivacyButtonStatus += GoogleMobileAdsConsentController_OnChangePrivacyButtonStatus;
        }


        private void OnDisable()
        {
            GoogleMobileAdsConsentController.OnChangePrivacyButtonStatus -= GoogleMobileAdsConsentController_OnChangePrivacyButtonStatus;
        }

        private void GoogleMobileAdsConsentController_OnChangePrivacyButtonStatus()
        {
            UpdateStatusButton();
        }

        private void UpdateStatusButton()
        {
            bool status = GoogleMobileAdsConsentController.Instance.PrivacyButtonStatus;
            _buttonPrivacyOption.gameObject.SetActive(status);
            if (status)
                _onButtonStatusOn?.Invoke();
            else _onButtonStatusOff?.Invoke();
        }
    }
}