public interface IResourceLogic
{
    ResourceChangeResult Resolve(ResourceChangeOrder order, Resource resource);
}
