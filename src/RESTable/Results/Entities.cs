using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc cref="OK" />
/// <summary>
///     A result that contains a set of entities
/// </summary>
public class Entities<T> : Content, IEntities<T>, IAsyncEnumerable<T> where T : class
{
    public Entities(IRequest request, IAsyncEnumerable<T> enumerable) : base(request)
    {
        Content = enumerable;
        Headers["Content-Disposition"] = $"attachment;filename={Request.Resource}_{DateTime.UtcNow:yyMMddHHmmssfff}" +
                                         $"{request.OutputContentTypeProvider.ContentDispositionFileExtension}";
    }

    /// <summary>
    ///     The entities contained in the result
    /// </summary>
    private IAsyncEnumerable<T> Content { get; }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        return Content.GetAsyncEnumerator(cancellationToken);
    }

    public Type EntityType => typeof(T);

    IEntities<T> IEntities<T>.Result => this;

    public ValueTask<long> CountAsync()
    {
        return Content.LongCountAsync();
    }

    /// <inheritdoc />
    public override string Metadata => $"{nameof(Entities<T>)};{Request.Resource};{EntityType}";
}