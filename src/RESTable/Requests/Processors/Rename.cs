using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RESTable.Meta;

namespace RESTable.Requests.Processors
{
    /// <inheritdoc cref="Dictionary{TKey,TValue}" />
    /// /// <inheritdoc cref="IProcessor" />
    /// <summary>
    /// Renames properties in an IEnumerable
    /// </summary>
    public class Rename : Dictionary<Term, string>, IProcessor
    {
        internal Rename(IEnumerable<(Term term, string newName)> terms, out ICollection<string> dynamicDomain)
        {
            foreach (var (term, newName) in terms)
                Add(term, newName);
            dynamicDomain = Values;
        }

        private Rename(Rename other) : base(other) { }

        internal Rename GetCopy() => new(this);

        private async ValueTask<ProcessedEntity> Renamed(ProcessedEntity entity)
        {
            foreach (var (key, newName) in this)
            {
                if (!entity.TryGetValue(key.Key, out var value))
                {
                    var termValue = await key.GetValue(entity).ConfigureAwait(false);
                    entity[newName] = termValue.Value;
                    continue;
                }
                entity.Remove(key.Key);
                entity[newName] = value;
            }
            return entity;
        }

        /// <summary>
        /// Renames properties in an IEnumerable
        /// </summary>
        public async IAsyncEnumerable<ProcessedEntity> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull
        {
            var typeCache = ApplicationServicesAccessor.GetRequiredService<TypeCache>();
            await foreach (var entity in entities)
            {
                if (entity is null) throw new ArgumentNullException(nameof(entities));
                var dictionary = await entity.MakeProcessedEntity(typeCache).ConfigureAwait(false);
                yield return await Renamed(dictionary).ConfigureAwait(false);
            }
        }
    }
}