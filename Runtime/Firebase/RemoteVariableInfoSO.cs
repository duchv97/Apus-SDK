using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Firebase.RemoteConfig;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "RemoteVariableInfoSO", menuName = "GAME/RemoteVariableInfoSO")]
public class RemoteVariableInfoSO : ScriptableObject
{
    [ListDrawerSettings(ListElementLabelName = "Name")]
    [SerializeField] private RemoteVariableInfo[] _remoteVariableInfos;

    public RemoteVariableInfo[] RemoteVariableInfos => _remoteVariableInfos;

#if UNITY_EDITOR
    [Button(ButtonSizes.Gigantic)]
    private void GenEnumFile()
    {
        const string enumName = "EnumRemoteVariable";
        const string filePath = "Assets/_Apus3rd/Firebase/EnumRemoteVariable.cs";

        var enumEntries = new List<string>();
        foreach (var item in _remoteVariableInfos)
            enumEntries.Add(item.Name);

        using var writer = new StreamWriter(filePath);
        writer.WriteLine($"public enum {enumName}");
        writer.WriteLine("{");

        foreach (var entry in enumEntries)
            writer.WriteLine($"\t{entry},");

        writer.WriteLine("}");

        AssetDatabase.Refresh();
    }
#endif
}

[System.Serializable]
public class RemoteVariableInfo
{
    public string Name;
    public EnumVariable TypeVariable;

    [ShowIf(nameof(TypeVariable), EnumVariable.BooleanValue)] public bool DefaultValueBoolean;
    [ShowIf(nameof(TypeVariable), EnumVariable.DoubleValue)] public double DefaultValueDouble;
    [ShowIf(nameof(TypeVariable), EnumVariable.LongValue)] public long DefaultValueLong;
    [ShowIf(nameof(TypeVariable), EnumVariable.StringValue)] public string DefaultValueString;

    private bool _valueBoolean;
    private double _valueDouble;
    private long _valueLong;
    private string _valueString;

    public void SaveData()
    {
        string key = $"RemoteVariable_{Name}";
        switch (TypeVariable)
        {
            case EnumVariable.BooleanValue:
                PlayerPrefs.SetInt(key, _valueBoolean ? 1 : 0);
                break;
            case EnumVariable.DoubleValue:
                PlayerPrefs.SetFloat(key, (float)_valueDouble);
                break;
            case EnumVariable.LongValue:
                PlayerPrefs.SetInt(key, (int)_valueLong);
                break;
            case EnumVariable.StringValue:
                PlayerPrefs.SetString(key, _valueString);
                break;
        }
    }

    public void LoadData()
    {
        string key = $"RemoteVariable_{Name}";
        switch (TypeVariable)
        {
            case EnumVariable.BooleanValue:
                _valueBoolean = PlayerPrefs.GetInt(key, DefaultValueBoolean ? 1 : 0) == 1;
                break;
            case EnumVariable.DoubleValue:
                _valueDouble = PlayerPrefs.GetFloat(key, (float)DefaultValueDouble);
                break;
            case EnumVariable.LongValue:
                _valueLong = PlayerPrefs.GetInt(key, (int)DefaultValueLong);
                break;
            case EnumVariable.StringValue:
                _valueString = PlayerPrefs.GetString(key, defaultValue: DefaultValueString);
                break;
        }
    }

    public object GetValue()
    {
        return TypeVariable switch
        {
            EnumVariable.BooleanValue => _valueBoolean,
            EnumVariable.DoubleValue => _valueDouble,
            EnumVariable.LongValue => _valueLong,
            EnumVariable.StringValue => _valueString,
            _ => null
        };
    }

    public void SetValue(ConfigValue value)
    {
        switch (TypeVariable)
        {
            case EnumVariable.BooleanValue:
                _valueBoolean = value.BooleanValue;
                break;
            case EnumVariable.DoubleValue:
                _valueDouble = value.DoubleValue;
                break;
            case EnumVariable.LongValue:
                _valueLong = value.LongValue;
                break;
            case EnumVariable.StringValue:
                _valueString = value.StringValue;
                break;
        }
    }

    public enum EnumVariable
    {
        BooleanValue,
        DoubleValue,
        LongValue,
        StringValue
    }
}
