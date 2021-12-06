using System;

namespace RESTable.Resources;

/// <summary>
///     Instructs RESTable to ignore all properties of other types that are of this type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class RESTableIgnoreMembersWithTypeAttribute : Attribute { }