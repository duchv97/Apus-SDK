using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "IAPInfoSO", menuName = "IAP")]
public class IAPInfoSO : ScriptableObject
{
    public string ProductIdIos;
    public string ProductIdAndroid;
    public ProductType ProductType;
    public PayoutDefinition PayoutDefinition;
    public float Price;
    public string CurrencyCode;
    //public string PriceString;

    // [Header("Custom")]
    // public bool isTest = false;

    public event Action<IAPInfoSO> OnBuySuccess;

    public void BuySuccess()
    {
        OnBuySuccess?.Invoke(this);
        var customFields = new Dictionary<string, object> { { "Pack_Name", GetProductID() } };
        //Parameter levelParam = new Parameter("Level", DataManager.Instance.CurrentLevel.ToString());
        AnalyticsManager.Instance.LogEvent("IAP_Buy_Success", customFields);//, levelParam);
    }

    public string GetProductID()
    {
#if UNITY_ANDROID
        return ProductIdAndroid;
#else
        return ProductIdIos;
#endif
    }

    public string GetNumberFormat(float valueCheck)
    {
        if (Mathf.FloorToInt(valueCheck) != valueCheck)
            return valueCheck.ToString("N");
        else return valueCheck.ToString("N0");
    }

    public void LoadData()
    {
        Price = PlayerPrefs.GetFloat("IAPInfo_" + GetProductID() + "_Price", defaultValue: Price);
        CurrencyCode = PlayerPrefs.GetString("IAPInfo_" + GetProductID() + "_CurrencyCode", defaultValue: CurrencyCode);
    }

    public void SaveData()
    {
        PlayerPrefs.SetFloat("IAPInfo_" + GetProductID() + "_Price", Price);
        PlayerPrefs.SetString("IAPInfo_" + GetProductID() + "_CurrencyCode", CurrencyCode);
    }
}
