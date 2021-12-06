using System;
using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable;

[RESTable(GET)]
public class Time : ISelector<Time>
{
    public DateTimeOffset UtcNow => DateTime.UtcNow;
    public DateTimeOffset LocalNow => DateTime.Now;

    public IEnumerable<Time> Select(IRequest<Time> request)
    {
        yield return new Time();
    }
}