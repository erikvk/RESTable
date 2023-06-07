namespace RESTable.DefaultProtocol.Serialized;

/// <summary>
///     For type safety in serialized error classes (kept as interface to allow
///     classes to define their own order of properties)
/// </summary>
public interface ISerializedError : ISerialized
{
    public string? ErrorType { get; }
    public ErrorCodes ErrorCode { get; }
    public string Message { get; }
    public string? MoreInfoAt { get; }
    public string Timestamp { get; }
    public string? Uri { get; }
}
