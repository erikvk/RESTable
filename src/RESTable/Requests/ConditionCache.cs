using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RESTable.Requests
{
//    public class ConditionCacheAccessor
//    {
//        private IDictionary<Type, object> Caches { get; }
//
//        public ConditionCacheAccessor(IDictionary<Type, object> caches)
//        {
//            Caches = caches;
//        }
//
//        public ConditionCache<T> GetCache<T>() where T : class
//        {
//            if (!Caches.TryGetValue(typeof(T), out var cache))
//                cache = Caches[typeof(T)] = new ConditionCache<T>();
//            return (ConditionCache<T>) cache;
//        }
//    }

    public class ConditionCache<T> : IDictionary<IUriCondition, Condition<T>> where T : class
    {
        private IDictionary<IUriCondition, Condition<T>> Cache { get; }

        public ConditionCache()
        {
            Cache = new ConcurrentDictionary<IUriCondition, Condition<T>>(UriCondition.EqualityComparer);
        }

        #region IDictionary

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<IUriCondition, Condition<T>>> GetEnumerator() => Cache.GetEnumerator();
        public void Add(KeyValuePair<IUriCondition, Condition<T>> item) => Cache.Add(item);
        public void Clear() => Cache.Clear();
        public bool Contains(KeyValuePair<IUriCondition, Condition<T>> item) => Cache.Contains(item);
        public void CopyTo(KeyValuePair<IUriCondition, Condition<T>>[] array, int arrayIndex) => Cache.CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<IUriCondition, Condition<T>> item) => Cache.Remove(item);
        public int Count => Cache.Count;
        public bool IsReadOnly => Cache.IsReadOnly;
        public void Add(IUriCondition key, Condition<T> value) => Cache.Add(key, value);
        public bool ContainsKey(IUriCondition key) => Cache.ContainsKey(key);
        public bool Remove(IUriCondition key) => Cache.Remove(key);
        public bool TryGetValue(IUriCondition key, out Condition<T> value) => Cache.TryGetValue(key, out value);
        public ICollection<IUriCondition> Keys => Cache.Keys;
        public ICollection<Condition<T>> Values => Cache.Values;

        public Condition<T> this[IUriCondition key]
        {
            get => Cache[key];
            set => Cache[key] = value;
        }

        #endregion
    }
}