#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Linq;
using RESTable.Meta;

namespace RESTable.Requests
{
    /// <summary>
    /// Represents the task of buffering the result of a RESTable request for {T}. Operations are available to
    /// configure the buffer that will be generated, and when awaited, the buffer is generated and returned as
    /// a ReadOnlyMemory{T}.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct EntityBufferTask<T> where T : class
    {
        private readonly IRequest<T> Request;
        private readonly int Offset;
        private readonly int Limit;
        private readonly ImmutableList<Condition<T>> Conditions;

        public ValueTask<ReadOnlyMemory<T>> All => AsMemoryAsync();
        public ValueTask<T?> First => GetResultEntities().FirstOrDefaultAsync();
        public ValueTask<T?> Last => GetResultEntities().LastOrDefaultAsync();
        public ValueTask<ReadOnlyMemory<T>> Get(Range range) => Slice(range).AsMemoryAsync();
        public ValueTask<T?> Get(int index) => GetResultEntities().ElementAtOrDefaultAsync(index);
        public EntityBufferTask<T> this[Range range] => Slice(range);
        public EntityBufferTask<T> this[int start, int length] => Slice(start, length);

        public ValueTask<T?> this[int index] => Get(index);
        public ValueTask<T?> this[Index index] => Get(index);

        public ValueTask<T?> Get(Index index)
        {
            var range = index..Following(index);
            return Slice(range).First;
        }

        private static Index Following(Index index) => new(index.IsFromEnd ? index.Value - 1 : index.Value + 1, index.IsFromEnd);

        internal EntityBufferTask(IRequest<T> request)
        {
            Request = request;
            Offset = 0;
            Limit = -1;
            Conditions = ImmutableList<Condition<T>>.Empty;
        }

        internal EntityBufferTask(IRequest<T> request, int offset, int limit, ImmutableList<Condition<T>> conditions)
        {
            Request = request;
            Offset = offset;
            Limit = limit;
            Conditions = conditions;
        }

        /// <summary>
        /// Clears all conditions from this buffer task
        /// </summary>
        public EntityBufferTask<T> WithNoConditions()
        {
            return new EntityBufferTask<T>(Request, Offset, Limit, ImmutableList<Condition<T>>.Empty);
        }

        /// <summary>
        /// Clears all predicates from this buffer task
        /// </summary>
        public EntityBufferTask<T> WithNoPredicates()
        {
            return new EntityBufferTask<T>(Request, Offset, Limit, Conditions);
        }

        /// <summary>
        /// Adds a range of new conditions to this buffer task
        /// </summary>
        public EntityBufferTask<T> Where(params (string key, Operators op, object value)[] conditions)
        {
            var termFactory = Request.GetRequiredService<TermFactory>();
            var target = Request.Target;

            var converted = new Condition<T>[conditions.Length];
            for (var index = 0; index < conditions.Length; index += 1)
            {
                var (key, op, value) = conditions[index];
                converted[index] = new Condition<T>
                (
                    term: termFactory.MakeConditionTerm(target, key),
                    op: op,
                    value: value
                );
            }

            return Where(converted);
        }

        /// <summary>
        /// Adds a range of new conditions to this buffer task
        /// </summary>
        public EntityBufferTask<T> Where(IEnumerable<Condition<T>> conditions)
        {
            return new EntityBufferTask<T>(Request, Offset, Limit, Conditions.AddRange(conditions));
        }

        /// <summary>
        /// Adds a new condition to this buffer task
        /// </summary>
        public EntityBufferTask<T> Where(string key, Operators op, object value)
        {
            return Where((key, op, value));
        }

        /// <summary>
        /// Adds a new condition to this buffer task
        /// </summary>
        public EntityBufferTask<T> Where(Condition<T> condition)
        {
            return new EntityBufferTask<T>(Request, Offset, Limit, Conditions.Add(condition));
        }

        private IAsyncEnumerable<T> GetResultEntities()
        {
            Request.Conditions = Conditions.ToList();
            return Request
                .WithMethod(Method.GET)
                .WithUpdater(null)
                .WithOffsetAndLimit(Offset, Limit)
                .GetResultEntities();
        }

        public async ValueTask<T> Patch(Index index, T updatedItem)
        {
            var result = await Slice(index..Following(index)).Patch(updatedItem.ToAsyncSingleton());
            return result.Span[0];
        }

        public ValueTask<ReadOnlyMemory<T>> Patch(ReadOnlySpan<T> updatedBuffer)
        {
            var updatedBufferArray = updatedBuffer.ToArray().ToAsyncEnumerable();
            return Patch(updatedBufferArray);
        }

        /// <summary>
        /// Calls the resource Update() with the given buffer as input entities, for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(ReadOnlyMemory<T> updatedBuffer)
        {
            var enumerable = MemoryMarshal.ToEnumerable(updatedBuffer).ToAsyncEnumerable();
            return Patch(enumerable);
        }

        public ValueTask<ReadOnlyMemory<T>> Patch(Range range, ReadOnlySpan<T> updatedBuffer)
        {
            var updatedBufferArray = updatedBuffer.ToArray();
            return Slice(range).Patch(updatedBufferArray.ToAsyncEnumerable());
        }

        public ValueTask<ReadOnlyMemory<T>> Patch(Range range, ReadOnlyMemory<T> updatedBuffer) => Slice(range).Patch(updatedBuffer);

        private async ValueTask<ReadOnlyMemory<T>> Patch(IAsyncEnumerable<T> updatedBuffer)
        {
            Request.Conditions = Conditions.ToList();
            return await Request
                .WithMethod(Method.PATCH)
                .WithUpdater(_ => updatedBuffer)
                .WithOffsetAndLimit(Offset, Limit)
                .GetResultEntities()
                .ToArrayAsync();
        }

        public async ValueTask<ReadOnlyMemory<T>> AsMemoryAsync()
        {
            T[]? array;
            switch (Limit)
            {
                case < 0:
                    array = null;
                    break;
                case > 0:
                    array = new T[Limit];
                    break;
                case 0: return Array.Empty<T>();
            }

            var entities = GetResultEntities();

            if (array is null)
            {
                return await entities.ToArrayAsync().ConfigureAwait(false);
            }

            var i = 0;
            await foreach (var item in entities.ConfigureAwait(false))
            {
                array[i] = item;
                i += 1;
            }

            return array;
        }

        public EntityBufferTask<T> Slice(int offset) => Slice(offset..);
        public EntityBufferTask<T> Slice(int offset, int length) => Slice(offset..(offset + length));

        public EntityBufferTask<T> Slice(Range range)
        {
            var (offset, limit) = range.ToSlicedOffsetAndLimit(Offset, Limit);
            return new EntityBufferTask<T>(Request, offset, limit, Conditions);
        }

        public ValueTaskAwaiter<ReadOnlyMemory<T>> GetAwaiter() => AsMemoryAsync().GetAwaiter();
        public ConfiguredEntityBufferTask<T> ConfigureAwait(bool continueOnCapturedContext) => new(this, continueOnCapturedContext);
    }
}
#endif