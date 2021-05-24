using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.WebSockets;

namespace RESTable.Resources
{
    /// <inheritdoc cref="ICollection{T}" />
    /// <summary>
    /// A thread-safe set of terminals.
    /// </summary>
    public class TerminalSet<T> : ICombinedTerminal<T>, ICollection<T> where T : Terminal
    {
        private readonly IDictionary<T, byte> terminals;

        /// <summary>
        /// Creates a new <see cref="TerminalSet{T}"/>
        /// </summary>
        public TerminalSet() => terminals = new ConcurrentDictionary<T, byte>();
        
        /// <inheritdoc />
        public void Add(T item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            terminals[item] = byte.MinValue;
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            return terminals.ContainsKey(item);
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            return terminals.Remove(item);
        }

        /// <inheritdoc cref="ICollection{T}.Count" />
        public int Count => terminals.Count;

        private IEnumerable<IWebSocket> WebSockets => terminals
            .Keys
            .Select(t => t.GetWebSocket());

        /// <inheritdoc />
        public IWebSocket CombinedWebSocket => new WebSocketCombination(WebSockets);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => terminals.Keys.GetEnumerator();

        /// <inheritdoc />
        public bool IsReadOnly => terminals.IsReadOnly;

        /// <inheritdoc />
        public void Clear() => terminals.Clear();

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex) => terminals
            .CopyTo(array.Select(a => new KeyValuePair<T, byte>(a, byte.MinValue)).ToArray(), arrayIndex);
    }
}