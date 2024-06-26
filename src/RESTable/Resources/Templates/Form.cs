﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources.Operations;

namespace RESTable.Resources.Templates;

/// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
/// <inheritdoc cref="IAsyncUpdater{T}" />
/// <summary>
///     Represents a form resource that can be fetched, populated and returned
/// </summary>
public abstract class Form<T> : ISelector<T>, IUpdater<T>, IPropertyChangeNotifier where T : Form<T>, new()
{
    /// <summary>
    ///     Saved forms
    /// </summary>
    protected IDictionary<string, T> Forms { get; } = new ConcurrentDictionary<string, T>();

    IEnumerable<T> ISelector<T>.Select(IRequest<T> request)
    {
        if (!request.Cookies.TryGetValue("FormId", out var cookie))
        {
            cookie = new Cookie("FormId", Guid.NewGuid().ToString("N"));
            request.Cookies.Add(cookie);
        }

        if (!Forms.TryGetValue(cookie.Value!, out var form))
            form = Forms[cookie.Value!] = new T();

        yield return form;
    }

    IEnumerable<T> IUpdater<T>.Update(IRequest<T> request)
    {
        return request.GetInputEntities();
    }
}
