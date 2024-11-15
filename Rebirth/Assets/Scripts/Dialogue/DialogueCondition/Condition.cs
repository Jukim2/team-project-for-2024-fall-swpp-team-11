using UnityEngine;
using System;

public class ConditionNameAttribute : Attribute
{
    public string DisplayName { get; private set; }
    public ConditionNameAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

public class Condition : ScriptableObject
{
    virtual public bool IsConditionMet() { return true; }
}