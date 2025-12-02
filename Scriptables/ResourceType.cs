using UnityEngine;

[CreateAssetMenu(fileName = "ResourceType", menuName = "Systems/Resources/New Resource Type")]
public class ResourceType : ScriptableObject
{
    public string Name;
    public int DefaultMin = 0;
    public int DefaultMax = 100;
    public bool Regenerates = false;
}
