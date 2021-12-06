using System;
using RESTable.Resources;

namespace RESTable.Json.Tests;

public class ParameterizedConstructorOptionalsTestClass
{
    [RESTableConstructor]
    public ParameterizedConstructorOptionalsTestClass(int @int, DateTime dateTime, Holder<int> intHolder = null, string str = "Optional")
    {
        Str = str;
        Int = @int;
        DateTime = dateTime;
        IntHolder = intHolder;
    }

    public string Str { get; }
    public int Int { get; }
    public DateTime DateTime { get; }

    [RESTableMember(hideIfNull: true)] public Holder<int> IntHolder { get; }
}