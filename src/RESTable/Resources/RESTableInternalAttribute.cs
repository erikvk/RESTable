using System;

namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Registers a class as an internal RESTable resource, that can only be used in internal
///     requests (using the RESTable.Request`1 class). If no methods are provided in the
///     constructor, all methods are made available for this resource.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RESTableInternalAttribute : RESTableAttribute
{
    /// <inheritdoc />
    public RESTableInternalAttribute(params Method[] methodRestrictions) : base(methodRestrictions) { }
}