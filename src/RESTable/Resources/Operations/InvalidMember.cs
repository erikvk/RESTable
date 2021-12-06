using System;

namespace RESTable.Resources.Operations;

public readonly struct InvalidMember
{
    public Type EntityType { get; }
    public string MemberName { get; }
    public Type MemberType { get; }
    public string Message { get; }

    public InvalidMember(Type entityType, string memberName, Type memberType, string message)
    {
        EntityType = entityType;
        MemberName = memberName;
        MemberType = memberType;
        Message = message;
    }
}