using System.Collections;
using UnityEngine;
using System;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] private NotificationConfig _data;

    void Start()
    {
        NotificationHelper.Instance.Initialize(true);
        ScheduleNotification();
    }

    private void ScheduleNotification()
    {
        for (int i = 1; i <= 30; i++)
        {
            DateTime dateValue = DateTime.Now.AddDays(i);
            bool isNextMonth = false;
            if (dateValue.Day <= DateTime.Now.Day)
                isNextMonth = true;

            if (i % 4 == 1)
                SendNotification(GetFireTime(dateValue, dateValue.Hour + 1, isNextMonth));
            else if (i % 4 == 2)
                SendNotification(GetFireTime(dateValue, dateValue.Hour - 1, isNextMonth));
            else if (i % 4 == 3)
                SendNotification(GetFireTime(dateValue, dateValue.Hour + 2, isNextMonth));
            else
                SendNotification(GetFireTime(dateValue, dateValue.Hour - 2, isNextMonth));
        }
    }

    private void SendNotification(DateTime fireTime)
    {
        try
        {
            NotiContent notiContent = _data.NotiContents[UnityEngine.Random.Range(0, _data.NotiContents.Length)];
            string tittle = notiContent.Tittle;
            string des = notiContent.Des;
            TimeSpan timeDelayFromFireTime = fireTime.Subtract(DateTime.Now);
            NotificationHelper.Instance.SendNotification(tittle, des, timeDelayFromFireTime, null, null, "", null);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private DateTime GetFireTime(DateTime dateTarget, int hourTarget, bool isNextMonth)
    {
        DateTime dateTime = DateTime.Now
                                .AddDays(dateTarget.Day - DateTime.Now.Day)
                                .AddHours(hourTarget - DateTime.Now.Hour)
                                .AddMinutes(-DateTime.Now.Minute)
                                .AddSeconds(-DateTime.Now.Second);
        if (isNextMonth)
            dateTime = dateTime.AddMonths(1);
        return dateTime;
    }
}
