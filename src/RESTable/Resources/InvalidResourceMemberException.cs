namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an invalid member inside a resource declaration
/// </summary>
public class InvalidResourceMemberException : RESTableException
{
    internal InvalidResourceMemberException(string info) : base(ErrorCodes.InvalidResourceMember, info) { }
}
