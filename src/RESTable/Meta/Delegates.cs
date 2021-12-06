using System.Threading.Tasks;

namespace RESTable.Meta;

/// <summary>
///     Represents a getter for a property. This is an open delgate, taking a
///     target object.
/// </summary>
/// <param name="target">The target of the open delegate invocation</param>
/// <returns>The value of the property</returns>
public delegate ValueTask<object?> Getter(object target);

/// <summary>
///     Represents a setter for a property. This is an open delgate, taking a
///     target object and a value to assign to the property.
/// </summary>
/// <param name="target">The target of the open delegate invocation</param>
/// <param name="value">The value to set the property to</param>
public delegate ValueTask Setter(object target, object? value);

/// <summary>
///     Creates a new instance of some object type
/// </summary>
/// <returns></returns>
public delegate T ParameterlessConstructor<T>();

public delegate T ParameterizedConstructor<T>(object?[] parameters);