using System;

namespace RESTable
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class UseDefaultConverterAttribute : Attribute { }
}