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
        /// <summary>
        /// The underlying request used to generate the entities. Shared among all subtasks. Its state is
        /// always overwritten when used.
        /// </summary>
        private readonly IRequest<T> Request;

        /// <summary>
        /// The offset within the entities enumeration, from which to generate the buffer
        /// </summary>
        private readonly int Offset;

        /// <summary>
        /// The length/limit of the buffer
        /// </summary>
        private readonly int Limit;

        /// <summary>
        /// Conditions used to filter the entities ahead of generating the buffer
        /// </summary>
        private readonly ImmutableList<Condition<T>> Conditions;

        /// <summary>
        /// The entities currently selected by this buffer, which would be read into a buffer on await
        /// </summary>
        public IAsyncEnumerable<T> Entities
        {
            get
            {
                Request.Conditions = Conditions.ToList();
                return Request
                    .WithMethod(Method.GET)
                    .WithUpdater(null)
                    .WithOffsetAndLimit(Offset, Limit)
                    .GetResultEntities();
            }
        }

        /// <summary>
        /// Patches the resource of this buffer task with an updated buffer
        /// </summary>
        private async ValueTask<ReadOnlyMemory<T>> Patch(IAsyncEnumerable<T> updatedBuffer)
        {
            Request.Conditions = Conditions.ToList();
            return await Request
                .WithMethod(Method.PATCH)
                .WithUpdater(_ => updatedBuffer)
                .WithOffsetAndLimit(Offset, Limit)
                .GetResultEntities()
                .ToArrayAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates a buffer of all elements
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> All => AsReadOnlyMemoryAsync();

        /// <summary>
        /// Generates a buffer of all the elements within the given range
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Within(Range range) => Slice(range).AsReadOnlyMemoryAsync();

        /// <summary>
        /// Returns the entity at the given index
        /// </summary>
        public ValueTask<T> At(int index) => Entities.ElementAtAsync(index);

        /// <summary>
        /// Returns the entity at the given index
        /// </summary>
        public ValueTask<T> At(Index index) => index.IsFromEnd ? Slice(index..new Index(index.Value - 1, true)).Entities.FirstAsync() : At(index.Value);

        /// <summary>
        /// Slices this buffer to a new one with a given range
        /// </summary>
        public EntityBufferTask<T> this[Range range] => Slice(range);

        /// <summary>
        /// Slices this buffer to a new one with a given start and length
        /// </summary>
        public EntityBufferTask<T> this[int start, int length] => Slice(start, length);

        /// <summary>
        /// Returns the entity at the given index
        /// </summary>
        public ValueTask<T> this[int index] => At(index);

        /// <summary>
        /// Returns the entity at the given index
        /// </summary>
        public ValueTask<T> this[Index index] => At(index);

        /// <summary>
        /// Returns the entity at the given index, or default if there is no such entity
        /// </summary>
        public ValueTask<T?> TryAt(int index) => Entities.ElementAtOrDefaultAsync(index);

        /// <summary>
        /// Returns the entity at the given index, or default if there is no such entity
        /// </summary>
        public ValueTask<T?> TryAt(Index index) => index.IsFromEnd ? Slice(index..(index.Value - 1)).Entities.FirstOrDefaultAsync() : TryAt(index.Value);

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
        /// Creates a ReadOnlyMemory buffer from this buffer task
        /// </summary>
        /// <returns></returns>
        public async ValueTask<ReadOnlyMemory<T>> AsReadOnlyMemoryAsync()
        {
            switch (Limit)
            {
                case < 0: return await Entities.ToArrayAsync().ConfigureAwait(false);
                case 0: return Array.Empty<T>();
                case > 0:
                {
                    var array = new T[Limit];
                    var i = 0;
                    await foreach (var item in Entities.ConfigureAwait(false))
                    {
                        array[i] = item;
                        i += 1;
                    }
                    return array;
                }
            }
        }

        #region Conditions

        /// <summary>
        /// Clears all conditions from this buffer task
        /// </summary>
        public EntityBufferTask<T> WithNoConditions()
        {
            return new EntityBufferTask<T>(Request, Offset, Limit, ImmutableList<Condition<T>>.Empty);
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

        #endregion

        #region Patch

        /// <summary>
        /// Calls the resource Update() with the given entity as input, for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
        public async ValueTask<T> Patch(Index index, T updatedItem)
        {
            var result = await Slice(index..index.GetNext()).Patch(updatedItem.ToAsyncSingleton()).ConfigureAwait(false);
            return result.Span[0];
        }

        /// <summary>
        /// Calls the resource Update() with the given buffer as input, for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
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

        /// <summary>
        /// Slices the buffer task to the range and then calls the resource Update() with the
        /// given buffer as input, for the resources that require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(Range range, ReadOnlySpan<T> updatedBuffer)
        {
            var updatedBufferArray = updatedBuffer.ToArray();
            return Slice(range).Patch(updatedBufferArray.ToAsyncEnumerable());
        }

        /// <summary>
        /// Slices the buffer task to the range and then calls the resource Update() with the
        /// given buffer as input, for the resources that require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(Range range, ReadOnlyMemory<T> updatedBuffer) => Slice(range).Patch(updatedBuffer);

        #endregion

        #region Slice

        public EntityBufferTask<T> Slice(int offset) => Slice(offset..);

        public EntityBufferTask<T> Slice(int offset, int length) => Slice(offset..(offset + length));

        public EntityBufferTask<T> Slice(Range range)
        {
            var (offset, limit) = range.ToSlicedOffsetAndLimit(Offset, Limit);
            return new EntityBufferTask<T>(Request, offset, limit, Conditions);
        }

        #endregion

        public ValueTaskAwaiter<ReadOnlyMemory<T>> GetAwaiter() => AsReadOnlyMemoryAsync().GetAwaiter();

        public ConfiguredEntityBufferTask<T> ConfigureAwait(bool continueOnCapturedContext) => new(this, continueOnCapturedContext);
    }
}
#endif