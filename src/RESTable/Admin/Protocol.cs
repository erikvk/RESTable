using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable.DefaultProtocol;
using RESTable.Internal;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Admin;

/// <inheritdoc />
/// <summary>
///     Contains all the available protocols and content types for the current RESTable instance
/// </summary>
[RESTable(Method.GET, Description = description)]
public class Protocol : ISelector<Protocol>
{
    private const string description = "Contains all the available protocols and content types for the current RESTable instance";

    public Protocol(string name, string identifier, bool isDefault, IEnumerable<ContentTypeInfo> contentTypes)
    {
        Name = name;
        Identifier = identifier;
        IsDefault = isDefault;
        ContentTypes = contentTypes;
    }

    /// <summary>
    ///     The protocol name
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The identifier of the protocol
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    ///     Is this the default protocol?
    /// </summary>
    public bool IsDefault { get; }

    /// <summary>
    ///     The content types supported by this protocol
    /// </summary>
    public IEnumerable<ContentTypeInfo> ContentTypes { get; }

    /// <inheritdoc />
    public IEnumerable<Protocol> Select(IRequest<Protocol> request)
    {
        return request
            .GetRequiredService<ProtocolProviderManager>()
            .CachedProtocolProviders
            .Values
            .Distinct()
            .Select(cachedProvider => new Protocol
            (
                cachedProvider.ProtocolProvider.ProtocolName,
                cachedProvider.ProtocolProvider.ProtocolIdentifier,
                cachedProvider.ProtocolProvider is DefaultProtocolProvider,
                cachedProvider.InputMimeBindings.Values
                    .Union(cachedProvider.OutputMimeBindings.Values)
                    .Distinct()
                    .Select(provider => new ContentTypeInfo
                    (
                        provider.Name,
                        provider.ContentType.MediaType,
                        cachedProvider.InputMimeBindings.Values.Contains(provider),
                        cachedProvider.OutputMimeBindings.Values.Contains(provider),
                        cachedProvider.InputMimeBindings
                            .Where(binding => binding.Value.Equals(provider))
                            .Union(cachedProvider.OutputMimeBindings
                                .Where(binding => binding.Value.Equals(provider)))
                            .Select(binding => binding.Key)
                    ))
            ));
    }
}