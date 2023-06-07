using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results;

/// <inheritdoc cref="IResult" />
public abstract class Success : IResult
{
    protected Success(IProtocolHolder protocolHolder, Headers? headers = null)
    {
        StatusDescription = null!;
        ProtocolHolder = protocolHolder;
        Context = protocolHolder.Context;
        ExcludeHeaders = false;
        Headers = headers ?? new Headers();
        LogTime = DateTime.Now;
        IsSuccess = true;
    }

    /// <inheritdoc />
    [RESTableMember(true)]
    public RESTableContext Context { get; }

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public HttpStatusCode StatusCode { get; protected set; }

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public string StatusDescription { get; protected set; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public Headers Headers { get; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public abstract IRequest Request { get; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public IProtocolHolder ProtocolHolder { get; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public Cookies Cookies => Context.Client.Cookies;

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public virtual TimeSpan TimeElapsed => Request.TimeElapsed;

    /// <inheritdoc />
    [RESTableMember(true)]
    public virtual MessageType MessageType => MessageType.HttpOutput;

    /// <inheritdoc />
    public virtual ValueTask<string> GetLogMessage()
    {
        return new($"{StatusCode.ToCode()}: {StatusDescription}");
    }

    public ValueTask<string?> GetLogContent()
    {
        return new(default(string));
    }

    /// <inheritdoc />
    [RESTableMember(true)]
    public string? HeadersStringCache { get; set; }

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public bool IsSuccess { get; }

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public bool IsError => !IsSuccess;

    /// <inheritdoc />
    [RESTableMember(true)]
    public bool ExcludeHeaders { get; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public DateTime LogTime { get; }

    /// <inheritdoc />
    public virtual IEntities<T> ToEntities<T>() where T : class
    {
        return (Entities<T>) this;
    }

    /// <inheritdoc />
    public void ThrowIfError() { }

    /// <inheritdoc />
    public void Dispose()
    {
        Request.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Request.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    [RESTableMember(hide: true)]
    public virtual string Metadata => $"{GetType().Name};;";
}
