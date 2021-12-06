namespace RESTable.Meta;

/// <summary>
///     Represents the operation to attach to a PropertyChanged event handler in DeclaredProperty
/// </summary>
public delegate void PropertyChangeHandler
(
    DeclaredProperty property,
    object target,
    dynamic? oldValue,
    dynamic? newValue
);