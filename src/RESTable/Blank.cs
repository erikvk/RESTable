using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable;

/// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
/// <inheritdoc cref="IInserter{T}" />
/// <inheritdoc cref="IUpdater{T}" />
/// <inheritdoc cref="IDeleter{T}" />
/// <summary>
///     The Blank resource is a test and debug resource that does nothing at all.
/// </summary>
[RESTable(Description = description)]
public class Blank : ISelector<Blank>, IInserter<Blank>, IUpdater<Blank>, IDeleter<Blank>
{
    private const string description = "A test and debug entity resource that is just an empty set of entities with no properties";

    /// <inheritdoc />
    public int Delete(IRequest<Blank> request)
    {
        return 0;
    }

    /// <inheritdoc />
    public IEnumerable<Blank> Insert(IRequest<Blank> request)
    {
        yield break;
    }

    /// <inheritdoc />
    public IEnumerable<Blank> Select(IRequest<Blank> request)
    {
        yield break;
    }

    /// <inheritdoc />
    public IEnumerable<Blank> Update(IRequest<Blank> request)
    {
        yield break;
    }
}
