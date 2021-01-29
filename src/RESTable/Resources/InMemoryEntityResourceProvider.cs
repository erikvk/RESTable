using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;

namespace RESTable.Resources
{
    /// <summary>
    ///  Entity resource provider for entities stored in CLR memory
    /// </summary>
    internal class InMemoryEntityResourceProvider : EntityResourceProvider<object>
    {
        private static IDictionary<Type, IDictionary<object, byte>> InMemoryStorage { get; }

        protected override Type AttributeType => typeof(InMemoryAttribute);

        static InMemoryEntityResourceProvider()
        {
            InMemoryStorage = new ConcurrentDictionary<Type, IDictionary<object, byte>>();
        }

        private IDictionary<object, byte> GetStore<T>()
        {
            if (!InMemoryStorage.TryGetValue(typeof(T), out var entities))
                entities = InMemoryStorage[typeof(T)] = new ConcurrentDictionary<object, byte>();
            return entities;
        }

        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => GetStore<T>().Keys.Cast<T>();

        protected override int DefaultInsert<T>(IRequest<T> request)
        {
            var count = 0;
            var entities = GetStore<T>();
            foreach (var toAdd in request.GetInputEntities())
            {
                if (!entities.ContainsKey(toAdd))
                    entities.Add(toAdd, 0);
                count += 1;
            }
            return count;
        }

        protected override int DefaultUpdate<T>(IRequest<T> request) => request
            .GetInputEntities()
            .Count();

        protected override int DefaultDelete<T>(IRequest<T> request)
        {
            var count = 0;
            var entities = GetStore<T>();
            foreach (var toDelete in request.GetInputEntities())
            {
                entities.Remove(toDelete);
                count += 1;
            }
            return count;
        }
    }
}