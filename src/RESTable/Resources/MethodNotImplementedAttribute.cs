using System;

namespace RESTable.Resources;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
internal class MethodNotImplementedAttribute : Attribute { }