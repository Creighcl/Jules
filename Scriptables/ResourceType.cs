using UnityEngine;

[CreateAssetMenu(fileName = "ResourceType", menuName = "Systems/Resources/New Resource Type")]
public class ResourceType : ScriptableObject
{
    public string Name;
    /// <summary>
    /// The minimum and maximum values for this resource type. These act as defaults and can be overridden per instance.
    /// </summary>
    public int MinValue = 0;
    public int MaxValue = 100;
    public bool Regenerates = false;
}
