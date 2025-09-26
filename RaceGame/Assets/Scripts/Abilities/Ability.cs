using UnityEngine;

[CreateAssetMenu(fileName = "Ability", menuName = "Scriptable Objects/Ability")]
public class Ability : ScriptableObject
{
    public string Name;
    public string Description;
    public Sprite Icon;
    [Space]
    public string AbilityMethodName;
}
