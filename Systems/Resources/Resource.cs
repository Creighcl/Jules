using System;

[Serializable]
public class Resource
{
    public ResourceType Config;
    public int CurrentValue;
    public int MaxValue;

    public Resource(ResourceType config, int max) {
        Config = config;
        MaxValue = max;
        CurrentValue = max;
    }
}
