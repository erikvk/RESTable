﻿using System.Collections;
using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.WebSockets;

internal class CombinedTerminal<T> : ICombinedTerminal<T>, ITerminalCollection<T> where T : Terminal
{
    private CombinedTerminal(List<T> terminals, IWebSocket combinedWebSocket)
    {
        CombinedWebSocket = combinedWebSocket;
        Terminals = terminals;
    }

    /// <summary>
    ///     Creates a new <see cref="CombinedTerminal{T}" /> with all terminals from a given terminal collection.
    ///     This constructor is used by the activator for the generic service type.
    /// </summary>
    public CombinedTerminal(ITerminalCollection<T> terminals)
    {
        Terminals = new List<T>();
        var webSockets = new List<IWebSocket>();
        foreach (var terminal in terminals)
        {
            Terminals.Add(terminal);
            webSockets.Add(terminal.GetWebSocket());
        }
        CombinedWebSocket = new WebSocketCombination(webSockets.ToArray());
    }

    private List<T> Terminals { get; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Terminals.GetEnumerator();
    }

    public int Count => Terminals.Count;
    public IWebSocket CombinedWebSocket { get; }

    internal static CombinedTerminal<T> Create(IEnumerable<T> terminals)
    {
        var terminalList = new List<T>();
        var webSockets = new List<IWebSocket>();
        foreach (var terminal in terminals)
        {
            terminalList.Add(terminal);
            webSockets.Add(terminal.GetWebSocket());
        }
        return new CombinedTerminal<T>
        (
            terminalList,
            new WebSocketCombination(webSockets.ToArray())
        );
    }
}
