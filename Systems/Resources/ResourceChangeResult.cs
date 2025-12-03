public class ResourceChangeResult
{
    public Character Source;
    public Character Target;
    public ResourceType Resource;
    public int OriginalAmount;
    public int FinalAmount;
    public int NewValue;
    public bool WasMitigated;
    public Effect SourceEffect;

    public ResourceChangeResult(Character source, Character target, ResourceType resource, int originalAmount, int finalAmount, int newValue, Effect sourceEffect)
    {
        Source = source;
        Target = target;
        Resource = resource;
        OriginalAmount = originalAmount;
        FinalAmount = finalAmount;
        NewValue = newValue;
        SourceEffect = sourceEffect;
    }
}
