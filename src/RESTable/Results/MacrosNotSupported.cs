namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable cannot locate a macro based on some search string
    /// </summary>
    internal class MacrosNotSupported : NotFound
    {
        internal MacrosNotSupported() : base(ErrorCodes.UnknownMacro,
            $"This version of RESTable does not support macros") { }
    }
}