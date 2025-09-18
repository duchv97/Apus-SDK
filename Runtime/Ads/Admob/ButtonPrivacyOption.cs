using Apus.GoogleMobileAds;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Apus.GoogleMobileAds
{
    public class ButtonPrivacyOption : MonoBehaviour
    {
        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(() =>
            {
                GoogleMobileAdsController.Instance.OpenPrivacyOptions();
            });
        }
    }
}