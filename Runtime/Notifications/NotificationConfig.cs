using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotificationConfig : ScriptableObject
{
    [SerializeField] private NotiContent[] _notiContents;

    public NotiContent[] NotiContents { get => _notiContents; }
}

[System.Serializable]
public struct NotiContent
{
    public string Tittle;
    [Multiline] public string Des;
}
