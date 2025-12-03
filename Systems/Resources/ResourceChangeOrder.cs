using System.Collections.Generic;

public class ResourceChangeOrder
{
    public Character Source { get; set; }
    public Character Target { get; set; }
    public ResourceType Resource { get; set; }
    public int Amount { get; set; } // Negative = Remove/Damage, Positive = Add/Heal
    public List<string> Tags { get; set; } = new List<string>();
    public Effect SourceEffect { get; set; }

    public ResourceChangeOrder(Character source, Character target, ResourceType resource, int amount, Effect sourceEffect)
    {
        Source = source;
        Target = target;
        Resource = resource;
        Amount = amount;
        SourceEffect = sourceEffect;
    }
}
