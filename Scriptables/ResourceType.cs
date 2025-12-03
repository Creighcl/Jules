using UnityEngine;

[CreateAssetMenu(fileName = "ResourceType", menuName = "Systems/Resources/New Resource Type")]
public class ResourceType : ScriptableObject, IResourceType
{
    public string Name;
    public int DefaultMin = 0;
    public int DefaultMax = 100;
    public bool Regenerates = false;

    // Explicit interface implementation to map fields to properties
    string IResourceType.Name => Name;
    int IResourceType.DefaultMax => DefaultMax;
}
