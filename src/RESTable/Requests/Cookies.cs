using System;
using System.Collections.Generic;

namespace RESTable.Requests;

/// <inheritdoc />
/// <summary>
///     Represents a collection of cookies. Behaves as a set of Cookie objects, with
///     cookie name equality as equality comparison. Adding a cookie with a name that equals
///     the name of a cookie already in the Cookies collection, will replace the
///     existing cookie.
/// </summary>
public class Cookies : HashSet<Cookie>
{
#if NETSTANDARD2_0
    /// <summary>
    ///     Finds a cookie by name (case sensitive)
    /// </summary>
    /// <param name="cookieName">The name of the cookie to find</param>
    /// <returns>
    ///     The cookie with the given name, or throws an exception if
    ///     there is no cookie with the given name
    /// </returns>
    public Cookie this[string cookieName]
    {
        get
        {
            if (TryGetValue(cookieName, out var found))
                return found;
            throw new KeyNotFoundException($"Found no cookie with name '{cookieName}'");
        }
    }

    /// <summary>
    ///     Tries to find a cookie with the given name, and returns whether the operation was successful.
    /// </summary>
    /// <param name="cookieName">The name of the cookie to find</param>
    /// <param name="value">The found cookie</param>
    /// <returns></returns>
    public bool TryGetValue(string cookieName, out Cookie value)
    {
        if (!Contains(new Cookie(cookieName)))
        {
            value = default;
            return false;
        }
        value = System.Linq.Enumerable.First(this, c => c.Name == cookieName);
        return true;
    }
#else
    /// <summary>
    ///     Finds a cookie by name (case sensitive)
    /// </summary>
    /// <param name="cookieName">The name of the cookie to find</param>
    /// <returns>
    ///     The cookie with the given name, or throws an exception if
    ///     there is no cookie with the given name
    /// </returns>
    public Cookie this[string cookieName]
    {
        get
        {
            if (TryGetValue(new Cookie(cookieName), out var found))
                return found;
            throw new KeyNotFoundException($"Found no cookie with name '{cookieName}'");
        }
    }

    /// <summary>
    ///     Tries to find a cookie with the given name, and returns whether the operation was successful.
    /// </summary>
    /// <param name="cookieName">The name of the cookie to find</param>
    /// <param name="value">The found cookie</param>
    /// <returns></returns>
    public bool TryGetValue(string cookieName, out Cookie value)
    {
        return base.TryGetValue(new Cookie(cookieName), out value);
    }
#endif

    /// <summary>
    ///     Adds a new cookie with the given cookie name and value
    /// </summary>
    public bool Add
    (
        string name,
        string value,
        DateTime? expires = null,
        int? maxAge = null,
        string? domain = null,
        bool httpOnly = false,
        bool secure = false,
        string? path = null
    )
    {
        return Add(new Cookie(name, value, expires, maxAge, domain, httpOnly, secure, path));
    }

    /// <summary>
    ///     Converts this Cookie collection to a readonly cookie collection
    /// </summary>
    /// <returns></returns>
    public ReadonlyCookies AsReadonly()
    {
        return new(this);
    }

    /// <inheritdoc />
    public Cookies() { }

    private Cookies(Cookies other) : base(other) { }

    internal Cookies GetCopy()
    {
        return new(this);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Creates a new Cookies collection from an enumeration of cookie strings
    /// </summary>
    /// <param name="cookieStrings">The cookie strings to add to the collection</param>
    public Cookies(IEnumerable<string> cookieStrings)
    {
        foreach (var cookieString in cookieStrings)
            if (Cookie.TryParse(cookieString, out var cookie))
                Add(cookie);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Creates a new Cookies collection from an enumeration of cookie strings
    /// </summary>
    /// <param name="cookieStrings">The cookie strings to add to the collection</param>
    public Cookies(IEnumerable<KeyValuePair<string, string>> cookieStrings)
    {
        foreach (var (key, value) in cookieStrings)
            if (Cookie.TryParse(key + "=" + value, out var cookie))
                Add(cookie);
    }
}