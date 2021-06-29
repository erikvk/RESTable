using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RESTable.Resources.Operations
{
    public readonly struct InvalidEntity
    {
        public long? Index { get; }
        public List<InvalidMember> InvalidMembers { get; }

        public InvalidEntity(long index, List<InvalidMember> invalidMembers)
        {
            Index = index;
            InvalidMembers = invalidMembers;
        }

        public InvalidEntity(List<InvalidMember> invalidMembers)
        {
            Index = null;
            InvalidMembers = invalidMembers;
        }
        
        public override string ToString() => JsonConvert.SerializeObject(this, Formatting.None);
    }

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
}