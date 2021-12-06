using System.Collections;
using System.Collections.Generic;

namespace RESTable.Requests;

/// <inheritdoc />
public class ReadonlyCookies : IReadOnlyCollection<Cookie>
{
    public ReadonlyCookies(Cookies cookies)
    {
        Cookies = cookies;
    }

    private Cookies Cookies { get; }

    /// <summary>
    ///     Finds a cookie by name (case sensitive)
    /// </summary>
    /// <param name="cookieName">The name of the cookie to find</param>
    /// <returns>
    ///     The cookie with the given name, or throws an exception if
    ///     there is no cookie with the given name
    /// </returns>
    public Cookie this[string cookieName] => Cookies[cookieName];

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public IEnumerator<Cookie> GetEnumerator()
    {
        return Cookies.GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => Cookies.Count;

    /// <summary>
    ///     Tries to find a cookie with the given name, and returns whether the operation was successful.
    /// </summary>
    /// <param name="cookieName">The name of the cookie to find</param>
    /// <param name="value">The found cookie</param>
    /// <returns></returns>
    public bool TryGetValue(string cookieName, out Cookie value)
    {
        return Cookies.TryGetValue(cookieName, out value);
    }
}