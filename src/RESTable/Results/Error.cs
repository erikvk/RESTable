using System;
using System.Net;
using System.Threading.Tasks;
using RESTable.Requests;

namespace RESTable.Results;

/// <inheritdoc cref="Exception" />
/// <inheritdoc cref="ITraceable" />
/// <inheritdoc cref="ISerializedResult" />
/// <summary>
///     A super class for all custom RESTable exceptions
/// </summary>
public abstract class Error : RESTableException, IResult, ITraceable
{
    private readonly string? _logContent = null;

    internal Error(ErrorCodes code, string message) : base(code, message)
    {
        StatusDescription = null!;
        Context = null!;
        Request = null!;
        Headers.Info = Message;
    }

    internal Error(ErrorCodes code, string? message, Exception? ie) : base(code, message, ie)
    {
        StatusDescription = null!;
        Context = null!;
        Request = null!;
        if (message is null)
            Headers.Info = ie?.Message;
        else Headers.Info = message;
    }

    /// <inheritdoc />
    public HttpStatusCode StatusCode { get; protected set; }

    /// <inheritdoc />
    public string StatusDescription { get; protected set; }

    /// <inheritdoc />
    public Headers Headers { get; } = new();

    /// <inheritdoc />
    public Cookies Cookies { get; } = new();

    /// <inheritdoc />
    public bool IsSuccess => false;

    /// <inheritdoc />
    public bool IsError => true;

    /// <inheritdoc />
    public IEntities<T> ToEntities<T>() where T : class
    {
        throw this;
    }

    /// <inheritdoc />
    public void ThrowIfError()
    {
        throw this;
    }

    /// <inheritdoc />
    public IProtocolHolder ProtocolHolder => Request;

    /// <inheritdoc />
    public IRequest Request { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (Request is not null)
            Request.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (Request is not null)
            await Request.DisposeAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual string Metadata => $"{GetType().Name};;";

    /// <inheritdoc />
    /// <summary>
    ///     The time elapsed from the start of reqeust evaluation
    /// </summary>
    public TimeSpan TimeElapsed
    {
        get
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Request is not null)
                return Request.TimeElapsed;
            return default;
        }
    }

    #region ITraceable, ILogable

    internal void SetContext(RESTableContext context)
    {
        Context = context;
    }

    /// <inheritdoc />
    public RESTableContext Context { get; private set; }

    /// <inheritdoc />
    public MessageType MessageType => MessageType.HttpOutput;

    /// <inheritdoc />
    public ValueTask<string> GetLogMessage()
    {
        var info = Headers.Info;
        var errorInfo = Headers.Error;
        var tail = "";
        if (info is not null)
            tail += $". {info}";
        if (errorInfo is not null)
            tail += $" (see {errorInfo})";
        return new ValueTask<string>($"{StatusCode.ToCode()}: {StatusDescription}{tail}");
    }

    /// <inheritdoc />
    public ValueTask<string?> GetLogContent()
    {
        return new(_logContent);
    }

    /// <inheritdoc />
    public DateTime LogTime { get; } = DateTime.Now;

    /// <inheritdoc />
    public string? HeadersStringCache { get; set; }

    /// <inheritdoc />
    public bool ExcludeHeaders => false;

    #endregion
}