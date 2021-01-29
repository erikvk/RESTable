using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Example
{
    /// <summary>
    ///  An example resource provider, here for an in-memory storage.
    /// </summary>
    public class InMemoryResourceProvider : EntityResourceProvider<object>
    {
        private static IDictionary<object, byte> InMemoryStorage { get; }

        protected override Type AttributeType { get; }

        static InMemoryResourceProvider()
        {
            InMemoryStorage = new ConcurrentDictionary<object, byte>();
        }

        public InMemoryResourceProvider()
        {
            AttributeType = typeof(InMemoryAttribute);
        }
        
        protected override IEnumerable<T> DefaultSelect<T>(IRequest<T> request) => InMemoryStorage.Keys.OfType<T>();

        protected override int DefaultInsert<T>(IRequest<T> request)
        {
            var count = 0;
            foreach (var toAdd in request.GetInputEntities())
            {
                if (!InMemoryStorage.ContainsKey(toAdd))
                    InMemoryStorage.Add(toAdd, 0);
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
            foreach (var toDelete in request.GetInputEntities())
            {
                InMemoryStorage.Remove(toDelete);
                count += 1;
            }
            return count;
        }
    }
}