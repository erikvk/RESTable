namespace RESTable.Meta;

/// <summary>
///     Defines the operation of handling a change observed by a property monitoring tree
/// </summary>
/// <param name="termRelativeRoot">A term representing the changing object, relative to the root</param>
/// <param name="oldValue">The old value of the changing object</param>
/// <param name="newValue">The new and current value of the changing object</param>
public delegate void ObservedChangeHandler(Term termRelativeRoot, object oldValue, object newValue);