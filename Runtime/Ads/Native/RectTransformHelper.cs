using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RectTransformHelper : MonoBehaviour
{
    private RectTransform _rect;
    [SerializeField] private Vector2 _position1;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();  
    }


    public void ChangePosition()
    {
        _rect.anchoredPosition = _position1;
    }
}
