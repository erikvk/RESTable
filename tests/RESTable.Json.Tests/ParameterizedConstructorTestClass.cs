using System;
using RESTable.Resources;

namespace RESTable.Json.Tests
{
    public class ParameterizedConstructorTestClass
    {
        public string Str { get; }
        public int Int { get; }
        public DateTime DateTime { get; }
        public Holder<int> IntHolder { get; }

        [RESTableConstructor]
        public ParameterizedConstructorTestClass(string str, int @int, DateTime dateTime, Holder<int> intHolder)
        {
            Str = str;
            Int = @int;
            DateTime = dateTime;
            IntHolder = intHolder;
        }
    }
}