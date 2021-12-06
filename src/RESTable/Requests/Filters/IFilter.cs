using System.Collections.Generic;

namespace RESTable.Requests.Filters;

internal interface IFilter
{
    IAsyncEnumerable<T> Apply<T>(IAsyncEnumerable<T> entities) where T : notnull;
}