using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RESTable.Meta;

namespace RESTable.Requests
{
    public class TermCache : IDictionary<(string Type, string Key, bool isInput), Term>
    {
        private IDictionary<(string Type, string Key, bool isInput), Term> Cache { get; }

        public void ClearTermsFor<T>() => Cache
            .Where(pair => pair.Key.Type == typeof(T).GetRESTableTypeName())
            .Select(pair => pair.Key)
            .ToList()
            .ForEach(key => Cache.Remove(key));

        public TermCache()
        {
            Cache = new ConcurrentDictionary<(string, string, bool), Term>();
        }

        #region IDictionary

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<KeyValuePair<(string Type, string Key, bool isInput), Term>> GetEnumerator() => Cache.GetEnumerator();
        public void Add(KeyValuePair<(string Type, string Key, bool isInput), Term> item) => Cache.Add(item);
        public void Clear() => Cache.Clear();
        public bool Contains(KeyValuePair<(string Type, string Key, bool isInput), Term> item) => Cache.Contains(item);
        public void CopyTo(KeyValuePair<(string Type, string Key, bool isInput), Term>[] array, int arrayIndex) => Cache.CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<(string Type, string Key, bool isInput), Term> item) => Cache.Remove(item);
        public int Count => Cache.Count;
        public bool IsReadOnly => Cache.IsReadOnly;
        public void Add((string Type, string Key, bool isInput) key, Term value) => Cache.Add(key, value);
        public bool ContainsKey((string Type, string Key, bool isInput) key) => Cache.ContainsKey(key);
        public bool Remove((string Type, string Key, bool isInput) key) => Cache.Remove(key);
        public bool TryGetValue((string Type, string Key, bool isInput) key, out Term value) => Cache.TryGetValue(key, out value!);
        public ICollection<(string Type, string Key, bool isInput)> Keys => Cache.Keys;
        public ICollection<Term> Values => Cache.Values;

        public Term this[(string Type, string Key, bool isInput) key]
        {
            get => Cache[key];
            set => Cache[key] = value;
        }

        #endregion
    }
}