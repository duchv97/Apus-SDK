using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeAdsUIContainer : MonoBehaviour
{
    [SerializeField] private GameObject _ads;

    private void OnEnable()
    {
        //PanelShop.OnShowHide += PanelShop_OnShowHide;
        //PanelSetting.OnShowHide += PanelSetting_OnShowHide;
        //UIButtonRevive.OnShowHide += UIButtonRevive_OnShowHide;
    }
    private void OnDisable()
    {
        //PanelShop.OnShowHide -= PanelShop_OnShowHide;
        //PanelSetting.OnShowHide -= PanelSetting_OnShowHide;
        //UIButtonRevive.OnShowHide -= UIButtonRevive_OnShowHide;
    }


    private void PanelSetting_OnShowHide(bool obj)
    {
        _ads.SetActive(!obj);
    }

    private void PanelShop_OnShowHide(bool obj)
    {
        _ads.SetActive(!obj);
    }

    private void UIButtonRevive_OnShowHide(bool obj)
    {
        _ads.SetActive(!obj);
    }
}
