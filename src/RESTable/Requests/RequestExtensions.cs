using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Results;

namespace RESTable.Requests
{
    public static class RequestExtensions
    {
        /// <summary>
        /// Evaluates the request asynchronously and gets its result, then
        /// serializes the result, optionally to the given output stream.
        /// </summary>
        public static async Task<ISerializedResult> GetAndSerializeResult(this IRequest request, Stream? customOutputStream = null, CancellationToken cancellationToken = new())
        {
            var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
            result.ThrowIfError();
            return await result.Serialize(customOutputStream, cancellationToken).ConfigureAwait(false);
        }

        public static async IAsyncEnumerable<T> GetResultEntities<T>(this IRequest<T> request, [EnumeratorCancellation] CancellationToken cancellationToken = new()) where T : class
        {
            await using var result = await request.GetResult(cancellationToken).ConfigureAwait(false);
            switch (result)
            {
                case Error error: throw error;
                case Change<T> change:
                {
                    foreach (var entity in change.Entities)
                        yield return entity;
                    yield break;
                }
                case IEntities<T> entities:
                {
                    await foreach (var entity in entities)
                        yield return entity;
                    yield break;
                }
                case var other: throw new InvalidOperationException($"Cannot convert result of type '{other.GetType()}' to an enumeration of entities");
            }
        }
    }
}