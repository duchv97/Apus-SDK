using UnityEngine;
using System;
using System.Collections;
#if UNITY_IOS
    using Unity.Notifications.iOS;
#endif
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif


public class NotificationHelper : MonoBehaviour
{
    const string channelID = "channel_id";
    
    public static NotificationHelper Instance;

#if UNITY_ANDROID
    private bool initialized;
    PermissionRequest request;
#endif

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Initializes notification channel and removes already scheduled notifications
    /// </summary>
    public void Initialize(bool cancelPendingNotifications)
    {
#if UNITY_ANDROID
        if (initialized == false)
        {
            initialized = true;
            var c = new AndroidNotificationChannel()
            {
                Id = channelID,
                Name = "Default Channel",
                Importance = Importance.High,
                Description = "Generic notifications",
            };
            AndroidNotificationCenter.RegisterNotificationChannel(c);
        }
        if (cancelPendingNotifications == true)
        {
            AndroidNotificationCenter.CancelAllNotifications();
        }
        RequestPermision(null);
#endif
#if UNITY_IOS
            if (cancelPendingNotifications == true)
            {
                iOSNotificationCenter.RemoveAllScheduledNotifications();
                iOSNotificationCenter.RemoveAllDeliveredNotifications();
            }
#endif
    }

    /// <summary>
    /// Schedules a notification
    /// </summary>
    /// <param name="title">title of the notification</param>
    /// <param name="text">body of the notification</param>
    /// <param name="timeDelayFromNow">time to appear, calculated from now</param>
    /// <param name="smallIcon">small icon name for android only - from Mobile Notification Settings </param>
    /// <param name="largeIcon">large icon name for android only - from Mobile Notification Settings </param>
    /// <param name="customData">custom data that can be retrieved when user opens the app from notification </param>
    internal void SendNotification(string title, string text, TimeSpan timeDelayFromNow, string smallIcon, string largeIcon, string customData, TimeSpan? repeatInterval)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification();
        notification.Title = title;
        notification.Text = text;
        if (repeatInterval != null)
        {
            notification.RepeatInterval = repeatInterval;
        }
        if (smallIcon != null)
        {
            notification.SmallIcon = smallIcon;
        }
        if (smallIcon != null)
        {
            notification.LargeIcon = largeIcon;
        }
        if (customData != null)
        {
            notification.IntentData = customData;
        }
        notification.FireTime = DateTime.Now.Add(timeDelayFromNow);

        AndroidNotificationCenter.SendNotification(notification, channelID);
#endif

#if UNITY_IOS
            iOSNotificationTimeIntervalTrigger timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = timeDelayFromNow,
                Repeats = false,
            };

            iOSNotification notification = new iOSNotification()
            {
                Title = title,
                Subtitle = "",
                Body = text,
                Data = customData,
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                CategoryIdentifier = "category_a",
                ThreadIdentifier = "thread1",
                Trigger = timeTrigger,
            };

            iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    /// <summary>
    /// Check if app was opened from notification
    /// </summary>
    /// <returns>the custom data from notification schedule or null if the app was not opened from notification</returns>
    public string AppWasOpenFromNotification()
    {
#if UNITY_ANDROID
        var notificationIntentData = AndroidNotificationCenter.GetLastNotificationIntent();

        if (notificationIntentData != null)
        {
            return notificationIntentData.Notification.IntentData;
        }
        else
        {
            return null;
        }
#elif UNITY_IOS
            iOSNotification notificationIntentData = iOSNotificationCenter.GetLastRespondedNotification();

            if (notificationIntentData != null)
            {
                return notificationIntentData.Data;
            }
            else
            {
                return null;
            }
#else
            return null;
#endif
    }
#if UNITY_ANDROID

    internal void RequestPermision(UnityEngine.Events.UnityAction<PermissionStatus> completeMethod)
    {
        StartCoroutine(RequestNotificationPermission(completeMethod));
    }

    IEnumerator RequestNotificationPermission(UnityEngine.Events.UnityAction<PermissionStatus> completeMethod)
    {
        request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
        {
            yield return null;
        }
        if (completeMethod != null)
        {
            completeMethod(request.Status);
        }
        // here use request.Status to determine users response
    }

    internal bool IsPermissionGranted()
    {
        return (request.Status == PermissionStatus.Allowed);
    }
#endif
}
