using System.Collections.Generic;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using static RESTable.Method;

namespace RESTable.Admin;

/// <summary>
///     Gets the configuration of the running RESTable application
/// </summary>
[RESTable(GET, Description = description)]
public class Configuration : ResourceWrapper<RESTableConfiguration>, ISelector<RESTableConfiguration>
{
    private const string description = "The Configuration resource contains the current " +
                                       "configuration for the RESTable instance.";

    public IEnumerable<RESTableConfiguration> Select(IRequest<RESTableConfiguration> request)
    {
        yield return request.Context.Configuration;
    }
}