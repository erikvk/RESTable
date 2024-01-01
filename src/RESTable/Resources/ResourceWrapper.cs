// ReSharper disable UnusedTypeParameter

using RESTable.Meta.Internal;

namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     ResourceWrapper instances enable resource declarations for resources in read-only
///     assemblies and/or in assemblies where a reference to RESTable is not practical.
/// </summary>
public abstract class ResourceWrapper<T> : IResourceWrapper where T : class;
