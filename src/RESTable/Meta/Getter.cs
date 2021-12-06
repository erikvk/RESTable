using System.Threading.Tasks;

namespace RESTable.Meta;

/// <summary>
///     Represents a getter for a property. This is an open delgate, taking a
///     target object.
/// </summary>
/// <param name="target">The target of the open delegate invocation</param>
/// <returns>The value of the property</returns>
public delegate ValueTask<TValue> Getter<TOwner, TValue>(TOwner TOwner);