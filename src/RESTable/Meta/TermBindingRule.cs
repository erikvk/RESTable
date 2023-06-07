namespace RESTable.Meta;

/// <summary>
///     The rule used for binding terms to a resource
/// </summary>
public enum TermBindingRule
{
    /// <summary>
    ///     First finds declared properties, then creates dynamic properties
    ///     for any unknown property
    /// </summary>
    DeclaredWithDynamicFallback,

    /// <summary>
    ///     Binds only declared properties, and throws an exception if no
    ///     declared property was found
    /// </summary>
    OnlyDeclared
}
