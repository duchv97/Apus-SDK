//using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingAOA : MonoBehaviour
{
    [SerializeField] private Image _imgFill;
    public void ShowLoading(float timeLoading)
    {
        gameObject.SetActive(true);
        _imgFill.fillAmount = 0;
        //_imgFill.DOFillAmount(1f, timeLoading).SetUpdate(true);
    }

    public void HideLoading()
    {
        gameObject.SetActive(false);
    }

    public void ForceLoadingDone(float timeLoading)
    {
        // DOTween.Kill(_imgFill);
        // _imgFill.DOFillAmount(1f, timeLoading).SetUpdate(true);
    }
}
