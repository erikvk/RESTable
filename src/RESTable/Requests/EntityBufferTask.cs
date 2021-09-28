#if !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Meta;
using RESTable.Results;
using Enumerable = System.Linq.Enumerable;

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
                    .WithSelector(null)
                    .WithUpdater(null)
                    .WithOffsetAndLimit(Offset, Limit)
                    .GetResultEntities();
            }
        }

        /// <summary>
        /// Patches the resource of this buffer task with an updated buffer
        /// </summary>
        private async ValueTask<ReadOnlyMemory<T>> InsertInternal(IEnumerable<T> toInsert)
        {
            Request.Conditions = Conditions.ToList();
            return await Request
                .WithMethod(Method.POST)
                .WithSelector(toInsert.ToAsyncEnumerable)
                .WithUpdater(null)
                .WithOffsetAndLimit(Offset, Limit)
                .GetResultEntities()
                .ToArrayAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Patches the resource of this buffer task with an updated buffer
        /// </summary>
        private async ValueTask<ReadOnlyMemory<T>> PatchInternal(IEnumerable<T> updatedBuffer)
        {
            Request.Conditions = Conditions.ToList();
            return await Request
                .WithMethod(Method.PATCH)
                .WithSelector(null)
                .WithUpdater(_ => updatedBuffer.ToAsyncEnumerable())
                .WithOffsetAndLimit(Offset, Limit)
                .GetResultEntities()
                .ToArrayAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Patches the resource of this buffer task with an updated buffer
        /// </summary>
        private async ValueTask<long> DeleteInternal()
        {
            Request.Conditions = Conditions.ToList();
            var result = await Request
                .WithMethod(Method.DELETE)
                .WithSelector(null)
                .WithUpdater(null)
                .WithOffsetAndLimit(Offset, Limit)
                .GetResultOrThrow<IChange<T>>()
                .ConfigureAwait(false);
            return result.Count;
        }

        /// <summary>
        /// Generates a buffer of all elements
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> All => AsReadOnlyMemoryAsync();

        /// <summary>
        /// Gets the first element selected by this buffer task
        /// </summary>
        public ValueTask<T> First => Entities.FirstAsync();

        /// <summary>
        /// Gets the last element selected by this buffer task
        /// </summary>
        public ValueTask<T> Last => Entities.LastAsync();

        /// <summary>
        /// Gets a raw memory buffer from this buffer task, possibly containing nulls
        /// if the selection limit is greater than the returned entity count.
        /// </summary>
        public ValueTask<Memory<T?>> Raw => AsRawMemoryAsync();

        /// <summary>
        /// Gets the first element selected by this buffer task
        /// </summary>
        public ValueTask<T?> TryFirst => Entities.FirstOrDefaultAsync();

        /// <summary>
        /// Gets the last element selected by this buffer task
        /// </summary>
        public ValueTask<T?> TryLast => Entities.LastOrDefaultAsync();

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
        public ValueTask<T> At(Index index) => Single(index).At(0);

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
        public ValueTask<T?> TryAt(Index index) => Single(index).TryAt(0);

        internal EntityBufferTask(IRequest<T> request)
        {
            Request = request;
            Request.MetaConditions.Unsafe = true;
            Offset = 0;
            Limit = -1;
            Conditions = ImmutableList<Condition<T>>.Empty;
        }

        internal EntityBufferTask(IRequest<T> request, int offset, int limit, ImmutableList<Condition<T>> conditions)
        {
            Request = request;
            Request.MetaConditions.Unsafe = true;
            Offset = offset;
            Limit = limit;
            Conditions = conditions;
        }

        /// <summary>
        /// Creates a raw memory buffer from this buffer task, possibly containing nulls if the
        /// selection limit is greater than the returned entity count.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<Memory<T?>> AsRawMemoryAsync()
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
                    return new ReadOnlyMemory<T>(array, 0, i);
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
        /// Calls the resource Update() with the provided entity as input for the resources that
        /// require Update() calls to mutate resource state, and returns true if and only if
        /// the entity was updated.
        /// </summary>
        public async ValueTask<bool> Patch(T updatedItem)
        {
            var buffer = await PatchInternal(Enumerable.Repeat(updatedItem, 1)).ConfigureAwait(false);
            return buffer.Length == 1;
        }

        /// <summary>
        /// Slices the buffer task to the given index, then calls the resource Update() with the provided entity as input for the resources that
        /// require Update() calls to mutate resource state, and returns true if and only if the entity was updated.
        /// </summary>
        public ValueTask<bool> Patch(Index index, T updatedItem) => Single(index).Patch(updatedItem);

        /// <summary>
        /// Calls the resource Update() with the provided entities as input for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(ReadOnlySpan<T> updatedBuffer) => PatchInternal(updatedBuffer.ToArray());

        /// <summary>
        /// Calls the resource Update() with the provided entities as input for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(ReadOnlyMemory<T> updatedBuffer) => PatchInternal(MemoryMarshal.ToEnumerable(updatedBuffer));

        /// <summary>
        /// Slices the buffer task to the given range, then calls the resource Update() with the provided entities as input for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(Range range, ReadOnlySpan<T> updatedBuffer) => Slice(range).PatchInternal(updatedBuffer.ToArray());

        /// <summary>
        /// Slices the buffer task to the given range, then calls the resource Update() with the provided entities as input for the resources that
        /// require Update() calls to mutate resource state.
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Patch(Range range, ReadOnlyMemory<T> updatedBuffer) => Slice(range).PatchInternal(updatedBuffer.ToArray());

        #endregion

        #region Put

        /// <summary>
        /// Inserts the entity if there is no existing entity in the resource at the place specified by this buffer task,
        /// or otherwise updates it to match the item provided. Returns true if and only if the resource is updated.
        /// </summary>
        public async ValueTask<bool> Put(T item)
        {
            if (await Entities.AnyAsync().ConfigureAwait(false))
                return await Patch(item).ConfigureAwait(false);
            return await Insert(item).ConfigureAwait(false);
        }

        /// <summary>
        /// Slices the buffer task to the given index, then inserts the entity if there is no existing entity in the
        /// resource at the place specified by that buffer task, or otherwise updates it to match the item provided.
        /// Returns true if and only if the resource is updated.
        /// </summary>
        public ValueTask<bool> Put(Index index, T item) => Single(index).Put(item);

        #endregion

        #region Insert

        /// <summary>
        /// Inserts the given entity into the resource and returns wether or not it was inserted successfully
        /// </summary>
        public async ValueTask<bool> Insert(T item)
        {
            var buffer = await InsertInternal(Enumerable.Repeat(item, 1)).ConfigureAwait(false);
            return buffer.Length == 1;
        }

        /// <summary>
        /// Inserts the given entities into the resource and returns a buffer of updated entities
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Insert(params T[] items) => InsertInternal(items);

        /// <summary>
        /// Inserts the given buffer of entities into the resource and return a buffer containing the inserted items
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Insert(ReadOnlyMemory<T> buffer) => InsertInternal(MemoryMarshal.ToEnumerable(buffer));

        /// <summary>
        /// Inserts the given buffer of entities into the resource and return a buffer containing the inserted items
        /// </summary>
        public ValueTask<ReadOnlyMemory<T>> Insert(ReadOnlySpan<T> buffer) => InsertInternal(buffer.ToArray());

        #endregion

        #region Delete

        /// <summary>
        /// Deletes the entities selected by this buffer task
        /// </summary>
        public ValueTask<long> Delete() => DeleteInternal();

        /// <summary>
        /// Deletes the entity at the given index
        /// </summary>
        public ValueTask<long> Delete(Index index) => Single(index).DeleteInternal();

        /// <summary>
        /// Slices the buffer task to the range and then deletes the entities selected by that buffer task
        /// </summary>
        public ValueTask<long> Delete(Range range) => Slice(range).DeleteInternal();

        #endregion

        #region Slice

        public EntityBufferTask<T> Slice(int offset) => Slice(offset..);

        public EntityBufferTask<T> Slice(int offset, int length) => Slice(offset..(offset + length));

        public EntityBufferTask<T> Single(Index index)
        {
            var range = index..new Index(index.IsFromEnd ? index.Value - 1 : index.Value + 1, index.IsFromEnd);
            var (offset, limit) = range.ToSlicedOffsetAndLimit(Offset, Limit);
            return new EntityBufferTask<T>(Request, offset, limit, Conditions);
        }

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