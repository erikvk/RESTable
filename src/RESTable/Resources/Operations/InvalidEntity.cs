﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RESTable.Resources.Operations;

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

    public override string ToString()
    {
        var data = InvalidMembers.ToDictionary(m => m.MemberName, m => m.Message);
        return JsonSerializer.Serialize(data);
    }
}
