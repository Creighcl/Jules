using System.Collections.Generic;

public class ResourceChangeOrder
{
    public Character Source;
    public Character Target;
    public ResourceType Resource;
    public int Amount; // Negative = Remove/Damage, Positive = Add/Heal
    public List<string> Tags = new List<string>();
    public Effect SourceEffect;

    public ResourceChangeOrder(Character source, Character target, ResourceType resource, int amount, Effect sourceEffect)
    {
        Source = source;
        Target = target;
        Resource = resource;
        Amount = amount;
        SourceEffect = sourceEffect;
    }
}
