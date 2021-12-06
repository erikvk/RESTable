using System;

namespace RESTable.Requests;

/// <summary>
///     The root context, capable of creating requests to all resources
/// </summary>
public class RootContext : RESTableContext
{
    public RootContext(RootClient rootClient, IServiceProvider services) : base(rootClient, services) { }
}