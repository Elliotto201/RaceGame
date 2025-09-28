using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class Ability : ScriptableObject
{
    public string Name;
    public string Description;
    public Sprite Icon;
    [Space]
    public string AbilityMethodName;
    [Header("Stat Modifications")]
    public StatModification[] StatModifications;
}

[Serializable]
public struct StatModification
{
    public StatType Type;
    [Range(-100, 100)]
    [Tooltip("The change in percent of the stat")]
    public int StatChange;
}

[Serializable]
public enum StatType
{
    Speed,
    Health,
}
