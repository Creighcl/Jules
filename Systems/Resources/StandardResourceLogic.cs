using System;

public class StandardResourceLogic : IResourceLogic
{
    public virtual ResourceChangeResult Resolve(ResourceChangeOrder order, Resource resource)
    {
        int originalAmount = order.Amount;
        int finalAmount = originalAmount;

        // Basic clamping
        int newValue = Math.Clamp(resource.CurrentValue + finalAmount, resource.Config.DefaultMin, resource.MaxValue);

        resource.CurrentValue = newValue;

        return new ResourceChangeResult(
            order.Source,
            order.Target,
            resource.Config,
            originalAmount,
            finalAmount,
            newValue,
            order.SourceEffect
        );
    }
}
