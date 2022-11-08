using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;
using static System.StringComparison;

namespace RESTable;

/// <inheritdoc cref="RESTable.Resources.Operations.ISelector{T}" />
/// <inheritdoc cref="Dictionary{TKey,TValue}" />
/// <summary>
///     A resource for creating arbitrary aggregated reports from multiple
///     internal requests.
/// </summary>
[RESTable(Method.GET, Method.POST, Description = description)]
public class Aggregator : Dictionary<string, object?>, IAsyncSelector<Aggregator>, IAsyncInserter<Aggregator>
{
    private const string description = "A resource for creating arbitrary aggregated reports from multiple internal requests";

    /// <inheritdoc />
    public async IAsyncEnumerable<Aggregator> SelectAsync(IRequest<Aggregator> request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var template = await request.Expecting
        (
            async r => await r.Body.DeserializeAsyncEnumerable<Aggregator>(cancellationToken).FirstAsync(cancellationToken).ConfigureAwait(false),
            "Expected an aggregator template as request body"
        ).ConfigureAwait(false);

        var jsonProvider = request.GetRequiredService<IJsonProvider>();

        async Task<object> Populator(object node)
        {
            switch (node)
            {
                case Aggregator aggregator:
                    foreach (var (key, obj) in aggregator.ToList())
                    {
                        var value = await Populator(obj!).ConfigureAwait(false);
                        switch (key)
                        {
                            case "$add" when IsNumberArray(value, out var terms): return terms.Sum();
                            case "$sub" when IsNumberArray(value, out var terms): return terms.Aggregate((x, y) => x - y);
                            case "$mul" when IsNumberArray(value, out var terms): return terms.Aggregate((x, y) => x * y);
                            case "$mod" when IsNumberArray(value, out var terms, 2): return terms[0] % terms[1];

                            case "$add":
                            case "$sub":
                            case "$mul": throw GetArithmeticException(key);
                            case "$mod": throw GetArithmeticException(key, "For $mod, the integer list must have a length of exactly 2");
                        }
                        aggregator[key] = value;
                    }
                    return aggregator;
                case JsonElement { ValueKind: JsonValueKind.Array } array:
                {
                    var list = new List<object>();
                    foreach (var item in array.EnumerateArray())
                    {
                        var obj = jsonProvider.ToObject<object>(item);
                        var populated = await Populator(obj!).ConfigureAwait(false);
                        list.Add(populated);
                    }
                    return list;
                }
                case JsonElement { ValueKind: JsonValueKind.Object } obj:
                {
                    return await Populator(jsonProvider.ToObject<Aggregator>(obj)!).ConfigureAwait(false);
                }
                case string empty when string.IsNullOrWhiteSpace(empty):
                {
                    return empty;
                }
                case string stringValue:
                {
                    Method method;
                    string uri;
                    if (stringValue.StartsWith("GET ", OrdinalIgnoreCase))
                    {
                        method = Method.GET;
                        uri = stringValue.Substring(4);
                    }
                    else if (stringValue.StartsWith("REPORT ", OrdinalIgnoreCase))
                    {
                        method = Method.REPORT;
                        uri = stringValue.Substring(7);
                    }
                    else
                    {
                        return stringValue;
                    }
                    if (string.IsNullOrWhiteSpace(uri))
                        throw new Exception($"Invalid URI in aggregator template. Expected relative uri after '{method.ToString()}'.");
                    var result = await request.Context
                        .CreateRequest(method, uri, request.Headers)
                        .GetResult(cancellationToken)
                        .ConfigureAwait(false);
                    return result switch
                    {
                        Error error => throw new Exception($"Could not get source data from '{uri}'. The resource returned: {error}"),
                        Report report => report.Count,
                        IEntities<object> entities => entities.ToEnumerable(),
                        var other => throw new Exception($"Unexpected result from {method.ToString()} request inside " + $"Aggregator: {await other.GetLogMessage()}")
                    };
                }
                case var other: return other;
            }
        }

        yield return await Populator(template).ConfigureAwait(false) switch
        {
            Aggregator aggregator => aggregator,
            long integer => new Aggregator { ["Result"] = integer },
            var other => throw new InvalidOperationException($"An error occured when reading the request template, the root object was resolved to {other.GetType().FullName}")
        };
    }

    public IAsyncEnumerable<Aggregator> InsertAsync(IRequest<Aggregator> request, CancellationToken cancellationToken) => SelectAsync(request, cancellationToken);

    private static Exception GetArithmeticException(string operation, string? message = null)
    {
        return new InvalidOperationException($"Invalid arguments for operation '{operation}'. The value for key '{operation}' must evaluate " +
                                             $"to a list of integers. {message}");
    }

    private static bool IsNumberArray(object _value, out long[] array, int? ofLength = null)
    {
        try
        {
            array = ((IList) _value).Cast<long>().ToArray();
            if (ofLength.HasValue) return array.Length == ofLength.Value;
            return true;
        }
        catch (InvalidCastException)
        {
            array = Array.Empty<long>();
            return false;
        }
    }
}
