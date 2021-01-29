﻿namespace RESTable.Resources
{
    /// <summary>
    /// The base type for .NET interfaces that define a custom set of members to use when defining
    /// an entity's members in RESTable. By making a type implement <see cref="IEntityDefinition"/>,
    /// we can define the members that RESTable will recognize for any type implementing it,
    /// without changing the actual definition of that type.
    /// </summary>
    public interface IEntityDefinition { }
}