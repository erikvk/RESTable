using System;
using System.Reflection;

namespace RESTable.Meta;

/// <inheritdoc />
/// <summary>
///     Encodes a field in a type
/// </summary>
public class Field : Member
{
    internal Field(FieldInfo fieldInfo) : base(fieldInfo.DeclaringType)
    {
        Name = fieldInfo.RESTableMemberName();
        ActualName = fieldInfo.Name;
        IsReadable = IsWritable = true;
        if (fieldInfo.IsInitOnly)
            IsWritable = false;
        Type = fieldInfo.FieldType;
        IsNullable = !fieldInfo.FieldType.IsValueType || fieldInfo.FieldType.IsNullable(out _);
        IsEnum = fieldInfo.FieldType.IsEnum || fieldInfo.FieldType.IsNullable(out var @base) && @base!.IsEnum;
    }

    /// <inheritdoc />
    public sealed override string Name { get; internal set; }

    /// <inheritdoc />
    public sealed override string ActualName { get; internal set; }

    /// <inheritdoc />
    public sealed override Type Type { get; protected set; }

    /// <inheritdoc />
    public override bool IsReadable { get; }

    /// <inheritdoc />
    public override bool IsWritable { get; }
}