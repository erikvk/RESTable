namespace RESTar.Resources
{
    /// <summary>
    /// The base type for .NET interfaces that define a custom set of members to use in the RESTar
    /// REST API. By having an entity resource implement an IEntityResourceInterface, and create
    /// explicit implementations of its properties, we can define its members without changing the
    /// actual format of the entity resource type.
    /// </summary>
    public interface IEntityResourceInterface { }
}