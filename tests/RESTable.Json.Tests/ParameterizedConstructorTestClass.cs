using System;
using System.Collections.Generic;
using RESTable.Resources;

namespace RESTable.Json.Tests;

public class ParameterizedConstructorTestClass
{
    [RESTableConstructor]
    public ParameterizedConstructorTestClass(string str, int @int, DateTime dateTime, Holder<int> intHolder)
    {
        Str = str;
        Int = @int;
        DateTime = dateTime;
        IntHolder = intHolder;
    }

    public string Str { get; }
    public int Int { get; }
    public DateTime DateTime { get; }
    public Holder<int> IntHolder { get; }
}

public class ParameterizedConstructorTestDictionaryClass : Dictionary<string, object>
{
    [RESTableConstructor]
    public ParameterizedConstructorTestDictionaryClass(string str, int @int, DateTime dateTime, Holder<int> intHolder)
    {
        Str = str;
        Int = @int;
        DateTime = dateTime;
        IntHolder = intHolder;
    }

    public string Str { get; }
    public int Int { get; }
    public DateTime DateTime { get; }
    public Holder<int> IntHolder { get; }
}
