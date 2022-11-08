using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.Internal;
using RESTable.Meta;

namespace RESTable.Requests;

/// <inheritdoc cref="ILogable" />
/// <inheritdoc cref="ITraceable" />
/// <summary>
///     A class that defines the parameters of a call to a RESTable API. A unified
///     way to talk about the input to request evaluation, regardless of protocol
///     and web technologies.
/// </summary>
internal class RequestParameters : ILogable, IHeaderHolder, IProtocolHolder, ITraceable
{
    private Body _body;

    /// <summary>
    ///     Used when creating generic requests through the .NET API
    /// </summary>
    internal RequestParameters(RESTableContext context, Method method, IResource resource, string? protocolIdentifier = null, string? viewName = null)
    {
        Context = context;
        Method = method;
        Headers = new Headers();
        iresource = resource;
        Uri = new URI(resource.Name, viewName);
        var protocolController = context.GetRequiredService<ProtocolProviderManager>();
        ProtocolIdentifier = protocolIdentifier?.ToLower() ?? protocolController.DefaultProtocolProvider.ProtocolProvider.ProtocolIdentifier;
        CachedProtocolProvider = protocolController.ResolveCachedProtocolProvider(protocolIdentifier);
        InputContentTypeProvider = this.GetInputContentTypeProvider();
        OutputContentTypeProvider = this.GetOutputContentTypeProvider();
        _body = new Body(this);
    }

    /// <summary>
    ///     Used when creating parsed requests
    /// </summary>
    internal RequestParameters(RESTableContext context, Method method, string uri, Headers? headers, object? body)
    {
        Context = context;
        Method = method;
        Headers = headers ?? new Headers();
        Uri = URI.ParseInternal(uri, PercentCharsEscaped(headers), context, out var cachedProtocolProvider);
        CachedProtocolProvider = cachedProtocolProvider;
        ProtocolIdentifier = cachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
        _body = new Body(this, body);
        InputContentTypeProvider = null!;
        OutputContentTypeProvider = null!;
        try
        {
            InputContentTypeProvider = this.GetInputContentTypeProvider();
            OutputContentTypeProvider = this.GetOutputContentTypeProvider();
        }
        catch (Exception e)
        {
            Error = e.AsError();
            return;
        }
        var hasMacro = Uri.Macro is not null;
        if (hasMacro && Uri.Macro?.Headers is not null)
        {
            if (Uri.Macro.OverwriteHeaders)
                foreach (var (key, value) in Uri.Macro.Headers)
                    Headers[key] = value;
            else
                foreach (var (key, value) in Uri.Macro.Headers)
                {
                    var currentValue = Headers.SafeGet(key);
                    if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                        Headers[key] = value;
                }
        }
        UnparsedUri = uri;
        if (Uri.HasError)
        {
            Error = Uri.Error;
            return;
        }
        try
        {
            var _ = IResource;
        }
        catch (Exception e)
        {
            Error = e.AsError();
        }
    }

    /// <summary>
    ///     Used when performing CheckOrigin
    /// </summary>
    internal RequestParameters(RESTableContext context, string uri, Headers? headers)
    {
        Context = context;
        Headers = headers ?? new Headers();
        Uri = URI.ParseInternal(uri, PercentCharsEscaped(headers), context, out var cachedProtocolProvider);
        CachedProtocolProvider = cachedProtocolProvider;
        ProtocolIdentifier = cachedProtocolProvider.ProtocolProvider.ProtocolIdentifier;
        _body = new Body(this);
        InputContentTypeProvider = null!;
        OutputContentTypeProvider = null!;
        try
        {
            InputContentTypeProvider = this.GetInputContentTypeProvider();
            OutputContentTypeProvider = this.GetOutputContentTypeProvider();
        }
        catch (Exception e)
        {
            Error = e.AsError();
            return;
        }
        var hasMacro = Uri.Macro is not null;
        if (hasMacro && Uri.Macro?.Headers is not null)
        {
            if (Uri.Macro.OverwriteHeaders)
                foreach (var (key, value) in Uri.Macro.Headers)
                    Headers[key] = value;
            else
                foreach (var (key, value) in Uri.Macro.Headers)
                {
                    var currentValue = Headers.SafeGet(key);
                    if (string.IsNullOrWhiteSpace(currentValue) || currentValue == "*/*")
                        Headers[key] = value;
                }
        }
        UnparsedUri = uri;
        try
        {
            var _ = IResource;
        }
        catch (Exception e)
        {
            Error = e.AsError();
        }
        if (Error is null && Uri.HasError)
            Error = Uri.Error;
    }

    /// <summary>
    ///     The method to perform
    /// </summary>
    public Method Method { get; set; }

    private URI Uri { get; }

    /// <summary>
    ///     The uri components contained in the arguments
    /// </summary>
    public IUriComponents UriComponents => Uri;

    /// <summary>
    ///     Did the request contain a body?
    /// </summary>
    public bool HasBody => Body is { CanRead: true };

    /// <summary>
    ///     The object that should form the request body
    /// </summary>
    public Body Body
    {
        get => _body;
        set
        {
            if (Equals(_body, value)) return;
            _body.DisposeAsync().AsTask().Wait();
            _body = value;
        }
    }

    /// <summary>
    ///     Are these request parameters valid?
    /// </summary>
    public bool IsValid => Error is null;

    /// <inheritdoc />
    public Headers Headers { get; }

    /// <inheritdoc />
    public RESTableContext Context { get; }

    public string ProtocolIdentifier { get; }

    public IContentTypeProvider InputContentTypeProvider { get; }

    public IContentTypeProvider OutputContentTypeProvider { get; }

    #region Private and internal

    private string? UnparsedUri { get; }
    internal IResource? iresource;
    internal IResource IResource => iresource ??= Context.GetRequiredService<ResourceCollection>().FindResource(Uri.ResourceSpecifier);
    internal Exception? Error { get; }

    private static bool PercentCharsEscaped(Headers? headers)
    {
        return headers?.ContainsKey("X-ARR-LOG-ID") == true;
    }

    bool IHeaderHolder.ExcludeHeaders => IResource is IEntityResource { RequiresAuthentication: true };
    public MessageType MessageType { get; } = MessageType.HttpInput;

    public CachedProtocolProvider CachedProtocolProvider { get; set; }

    ValueTask<string> ILogable.GetLogMessage()
    {
        var message = $"{Method} {UnparsedUri}";
        if (HasBody)
            return new ValueTask<string>(message + Body.GetLengthLogString());
        return new ValueTask<string>(message);
    }

    DateTime ILogable.LogTime { get; } = DateTime.Now;
    public string? HeadersStringCache { get; set; }

    async ValueTask<string?> ILogable.GetLogContent()
    {
        if (!HasBody) return null;
        return await Body.ToStringAsync().ConfigureAwait(false);
    }

    #endregion
}
