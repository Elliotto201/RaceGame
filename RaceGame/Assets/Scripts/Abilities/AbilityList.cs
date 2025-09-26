using UnityEngine;

[CreateAssetMenu(fileName = "AbilityList", menuName = "Scriptable Objects/AbilityList")]
public class AbilityList : ScriptableObject
{
    public Ability[] Abilities;
}
