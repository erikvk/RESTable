using System;
using System.Text.Json.Serialization;
using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized;

public class SerializedError : ISerializedError
{
    public SerializedError(Error error, string? uri)
    {
        Error = error;
        Uri = uri;
    }

    private Error Error { get; }

    public double TimeElapsedMs => Error.TimeElapsed.GetRESTableElapsedMs();

    public string Status => "fail";
    public string? ErrorType => Error.GetType().FullName;

    public ErrorCodes ErrorCode => Error.ErrorCode;

    public string Message => Error.Message;

    public string? MoreInfoAt => Error.Headers.Error;

    public string Timestamp => DateTime.UtcNow.ToString("O");

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uri { get; }
}
