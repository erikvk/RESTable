using System.Reflection;

namespace RESTar.Reflection.Dynamic
{
    /// <inheritdoc />
    /// <summary>
    /// Encodes a field in a type
    /// </summary>
    public class Field : Member
    {
        /// <inheritdoc />
        public override bool Readable { get; }

        /// <inheritdoc />
        public override bool Writable { get; }

        internal Field(FieldInfo fieldInfo)
        {
            Name = fieldInfo.RESTarMemberName();
            ActualName = fieldInfo.Name;
            Readable = Writable = true;
            if (fieldInfo.IsInitOnly)
                Writable = false;
            Type = fieldInfo.FieldType;
            Nullable = !fieldInfo.FieldType.IsValueType || fieldInfo.FieldType.IsNullable(out var _);
            IsEnum = fieldInfo.FieldType.IsEnum;
        }
    }
}