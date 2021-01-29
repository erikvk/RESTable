namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when an invalid enum declaration was referenced from a RESTable resource
    /// </summary>
    public class InvalidReferencedEnumDeclarationException : RESTableException
    {
        internal InvalidReferencedEnumDeclarationException(string info) : base(ErrorCodes.InvalidReferencedEnumDeclaration, info) { }
    }
}