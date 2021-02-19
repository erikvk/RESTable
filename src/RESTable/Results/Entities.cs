using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="OK" />
    /// <summary>
    /// A result that contains a set of entities
    /// </summary>
    public class Entities<T> : Content, IEntities<T> where T : class
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            return Content.GetAsyncEnumerator(cancellationToken);
        }

        /// <summary>
        /// The entities contained in the result
        /// </summary>
        private IAsyncEnumerable<T> Content { get; }

        public Type EntityType => typeof(T);

        internal Entities(IRequest request, IAsyncEnumerable<T> enumerable) : base(request) => Content = enumerable ?? new T[0].ToAsyncEnumerable();

        /// <inheritdoc />
        public override string Metadata => $"{nameof(Entities<T>)};{Request.Resource};{EntityType}";
    }
}