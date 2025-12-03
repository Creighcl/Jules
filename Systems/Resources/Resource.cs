using System;

[Serializable]
public class Resource
{
    // Now uses interface to avoid direct dependency on ScriptableObject
    public IResourceType Config;
    public int CurrentValue;
    public int MaxValue;

    public Resource(IResourceType config, int max) {
        Config = config;
        MaxValue = max;
        CurrentValue = max;
    }
}
