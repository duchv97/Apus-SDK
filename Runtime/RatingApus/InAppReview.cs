#if UNITY_ANDROID
using Google.Play.Review;
#endif
using System.Collections;
using UnityEngine;

public class InAppReview : MonoBehaviour
{
    public static InAppReview Instance;

    private void Awake()
    {
        Instance = this;
    }

#if UNITY_ANDROID
    private ReviewManager reviewManager;
    private PlayReviewInfo playReviewInfo;
#endif
    public void StartRequestReview()
    {
        StartCoroutine(RequestReview());
    }

    private IEnumerator RequestReview()
    {
#if UNITY_ANDROID
        reviewManager = new ReviewManager();
        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            // Log error. For example, using requestFlowOperation.Error.ToString().
            Application.OpenURL("market://details?id=" + Application.identifier);
            yield break;
        }
        playReviewInfo = requestFlowOperation.GetResult();
        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return launchFlowOperation;
        playReviewInfo = null; // Reset the object
        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            // Log error. For example, using requestFlowOperation.Error.ToString().
            Application.OpenURL("market://details?id=" + Application.identifier);
            yield break;
        }
        // The flow has finished. The API does not indicate whether the user
        // reviewed or not, or even whether the review dialog was shown. Thus, no
        // matter the result, we continue our app flow.
#else
            yield return null;
#endif
    }
}