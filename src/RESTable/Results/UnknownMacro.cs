namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable cannot locate a macro based on some search string
/// </summary>
internal class UnknownMacro : NotFound
{
    internal UnknownMacro(string searchString) : base(ErrorCodes.UnknownMacro,
        $"RESTable could not locate any valid macro by '{searchString}'. To list macros, use the 'RESTable.Admin.Macro' resource") { }
}