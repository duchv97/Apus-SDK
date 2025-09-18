using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NativeAdsBillboardClickChecker : MonoBehaviour
{
    public bool IsShow { get; private set; }

    private void OnEnable()
    {
        IsShow = true;
        NativeAdsBillboard.I.CheckState();
    }

    private void OnDisable()
    {
        IsShow = false;
        NativeAdsBillboard.I.CheckState();
    }
}
