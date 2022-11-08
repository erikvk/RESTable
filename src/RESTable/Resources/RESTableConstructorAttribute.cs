using System;

namespace RESTable.Resources;

/// <summary>
///     Marks this constructor as the one to use when deserializing instances of this type
///     in RESTable
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public sealed class RESTableConstructorAttribute : Attribute { }
