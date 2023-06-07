namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an invalid resource declaration
/// </summary>
public class InvalidResourceDeclarationException : RESTableException
{
    internal InvalidResourceDeclarationException(string info) : base(ErrorCodes.InvalidResourceDeclaration, info) { }
}
