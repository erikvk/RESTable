using System;
using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Returned to the client on successful deletion of entities
/// </summary>
public class DeletedEntities<T> : Change<T> where T : class
{
    public DeletedEntities(IRequest request, long count) : base(request, count, Array.Empty<T>())
    {
        DeletedCount = count;
        Headers.Info = $"{count} entities deleted from '{request.Resource}'";
    }

    /// <summary>
    ///     The number of entities deleted
    /// </summary>
    public long DeletedCount { get; }

    /// <inheritdoc />
    public override string Metadata => $"{nameof(DeletedEntities<T>)};{Request.Resource};{DeletedCount}";
}