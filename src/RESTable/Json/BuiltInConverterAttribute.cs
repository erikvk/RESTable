using System;

namespace RESTable.Json
{
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class BuiltInConverterAttribute : Attribute { }
}