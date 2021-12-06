﻿using System;
using System.Collections.Generic;

namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Registers a new RESTable resource and provides permissions. If no methods are
///     provided in the constructor, all methods are made available for this resource.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RESTableAttribute : Attribute
{
    /// <inheritdoc />
    /// <summary>
    ///     Registers a new RESTable resource and provides permissions. If no methods are
    ///     provided in the constructor, all methods are made available for this resource.
    /// </summary>
    public RESTableAttribute(params Method[] methodRestrictions)
    {
        AvailableMethods = methodRestrictions.ResolveMethodRestrictions();
    }

    /// <summary>
    ///     The methods declared as available for this RESTable resource. Not applicable for
    ///     terminal resources.
    /// </summary>
    public IReadOnlyList<Method> AvailableMethods { get; }

    /// <summary>
    ///     If true, unknown conditions encountered when handling incoming requests
    ///     will be passed through as dynamic. This allows for a dynamic handling of
    ///     members, both for condition matching and for entities returned from the
    ///     resource selector. Not applicable for terminal resources.
    /// </summary>
    public bool AllowDynamicConditions { get; set; }

    /// <summary>
    ///     Resource descriptions are visible in the AvailableResource resource
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Should this resource, with methods GET and REPORT, be included in all access scopes,
    ///     regardless of API keys in the configuration file? This is useful for global resources
    ///     in foreign assemblies. The API will still require API key if requireApiKey is set to
    ///     true in the call to RESTableConfig.Init(), but this resource will be included in each
    ///     key's scope.
    /// </summary>
    public bool GETAvailableToAll { get; set; }

    /// <summary>
    ///     Does this attribute describe a declared resource type?
    /// </summary>
    internal bool IsDeclared => this is not RESTableProceduralAttribute;
}