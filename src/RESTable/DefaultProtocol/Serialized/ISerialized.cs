namespace RESTable.DefaultProtocol.Serialized;

/// <summary>
///     For type safety in serialized classes (kept as interface to allow
///     classes to define their own order of properties)
/// </summary>
public interface ISerialized
{
    public string Status { get; }
}