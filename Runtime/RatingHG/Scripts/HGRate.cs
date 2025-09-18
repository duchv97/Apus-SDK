using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using  HG.Rate;
public class HGRate : MonoBehaviour
{
    [SerializeField] GameObject ratePrefab;
    [SerializeField] Transform canvasTrans;

    public void CallRatePopup()
    {
        var temp = Instantiate(ratePrefab, canvasTrans);
        temp.GetComponent<RateManager>().ShowPopup();
    }

}
