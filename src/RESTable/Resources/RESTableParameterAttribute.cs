using System;
using RESTable.Requests;

namespace RESTable.Resources
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RESTableParameterAttribute : RESTableMemberAttribute
    {
        public RESTableParameterAttribute(string? name = null) : base
        (
            name: name,
            hide: true,
            skipConditions: true,
            allowedOperators: Operators.EQUALS
        ) { }
    }
}