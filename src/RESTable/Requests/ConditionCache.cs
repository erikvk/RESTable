using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RESTable.Requests
{
    public interface IConditionCache : IEnumerable<ICondition>
    {
        Type ResourceType { get; }
        void Clear();
        int Count { get; }
    }

    public class ConditionCache<T> : ConcurrentDictionary<IUriCondition, Condition<T>>, IConditionCache where T : class
    {
        public ConditionCache() : base(UriCondition.EqualityComparer) { }

        #region IConditionCache

        public Type ResourceType => typeof(T);
        IEnumerator<ICondition> IEnumerable<ICondition>.GetEnumerator() => Values.GetEnumerator();

        #endregion
    }
}