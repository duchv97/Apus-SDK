using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPConfig : MonoBehaviour
{
    [SerializeField] private IAPInfoSO[] _iAPInfoSOs;
    public IAPInfoSO[] IAPInfoSOs => _iAPInfoSOs;

    public static IAPConfig I;

    private void Awake()
    {
        I = this;

#if !UNITY_EDITOR
        for (int i = 0; i < _iAPInfoSOs.Length; i++)
            _iAPInfoSOs[i].LoadData();
#endif
    }

    public void UpdateData(ProductCollection productCollection)
    {
        for (int i = 0; i < _iAPInfoSOs.Length; i++)
        {
            Product product = productCollection.WithID(_iAPInfoSOs[i].GetProductID());
            if (product != null)
            {
                _iAPInfoSOs[i].Price = (float)product.metadata.localizedPrice;
                if (!TryGetCurrencySymbol(product.metadata.isoCurrencyCode, out _iAPInfoSOs[i].CurrencyCode))
                    _iAPInfoSOs[i].CurrencyCode = GetCurrencySymbol(product.metadata.localizedPriceString);
                //_iAPInfoSOs[i].PriceString = product.metadata.localizedPriceString;
                _iAPInfoSOs[i].SaveData();
            }
        }
    }

    public bool TryGetCurrencySymbol(string isoCurrencyCode, out string symbol)
    {
        symbol = CultureInfo
            .GetCultures(CultureTypes.AllCultures)
            .Where(c => !c.IsNeutralCulture)
            .Select(culture =>
            {
                try
                {
                    return new RegionInfo(culture.Name);
                }
                catch
                {
                    return null;
                }
            })
            .Where(ri => ri != null && ri.ISOCurrencySymbol == isoCurrencyCode)
            .Select(ri => ri.CurrencySymbol)
            .FirstOrDefault();
        return symbol != null;
    }

    private string GetCurrencySymbol(string str)
    {
        int idNumber = 0;
        for (int i = 0; i < str.Length; i++)
        {
            if (char.IsDigit(str[i]))
            {
                idNumber = i;
                break;
            }
        }
        return str.Substring(0, idNumber);
    }
}