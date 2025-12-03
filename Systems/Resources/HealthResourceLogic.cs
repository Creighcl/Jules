public class HealthResourceLogic : StandardResourceLogic
{
    public override ResourceChangeResult Resolve(ResourceChangeOrder order, Resource resource)
    {
        ResourceChangeResult result = base.Resolve(order, resource);

        if (result.NewValue <= 0) {
            // Trigger death?
            // For now, we rely on the generic system to check "isDead" status elsewhere
            // or we could fire an event here if we had the event provider.
        }

        return result;
    }
}
