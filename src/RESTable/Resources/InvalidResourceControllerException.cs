namespace RESTable.Resources;

/// <inheritdoc />
/// <summary>
///     Thrown when an invalid resource controller has been declared
/// </summary>
public class InvalidResourceControllerException : RESTableException
{
    internal InvalidResourceControllerException(string info) : base(ErrorCodes.InvalidResourceControllerDeclaration, info) { }
}