using System.Collections;
using System.Collections.Generic;

namespace RESTable.Requests
{
    /// <inheritdoc />
    public class ReadonlyCookies : IReadOnlyCollection<Cookie>
    {
        private Cookies Cookies { get; }

        public ReadonlyCookies(Cookies cookies) => Cookies = cookies;

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<Cookie> GetEnumerator() => Cookies.GetEnumerator();

        /// <inheritdoc />
        public int Count => Cookies.Count;

        /// <summary>
        /// Finds a cookie by name (case sensitive)
        /// </summary>
        /// <param name="cookieName">The name of the cookie to find</param>
        /// <returns>The cookie with the given name, or throws an exception if
        /// there is no cookie with the given name</returns>
        public Cookie this[string cookieName] => Cookies[cookieName];

        /// <summary>
        /// Tries to find a cookie with the given name, and returns whether the operation was successful.
        /// </summary>
        /// <param name="cookieName">The name of the cookie to find</param>
        /// <param name="value">The found cookie</param>
        /// <returns></returns>
        public bool TryGetValue(string cookieName, out Cookie value) => Cookies.TryGetValue(cookieName, out value);
    }
}