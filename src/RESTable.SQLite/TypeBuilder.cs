using System;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.TypeAttributes;

namespace RESTable.Sqlite;

internal static class TypeBuilder
{
    private const string AssemblyNameString = "RESTable.Sqlite.Dynamic";

    static TypeBuilder()
    {
        AssemblyName = new AssemblyName(AssemblyNameString);
        AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(AssemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = AssemblyBuilder.DefineDynamicModule(AssemblyName.Name + ".dll");
    }

    private static AssemblyName AssemblyName { get; }
    private static AssemblyBuilder AssemblyBuilder { get; }
    private static ModuleBuilder ModuleBuilder { get; }
    internal static Assembly Assembly => AssemblyBuilder;

    internal static Type? GetType(ProceduralResource resource)
    {
        var existing = AssemblyBuilder.GetType(resource.Name);
        if (existing is not null) return existing;
        var baseType = resource.BaseTypeName is string baseTypeName ? Type.GetType(baseTypeName) : null;
        if (baseType is null) return null;
        return MakeType(resource.Name, baseType);
    }

    private static Type? MakeType(string name, Type baseType)
    {
        return ModuleBuilder
            .DefineType(name, Class | Public | Sealed, baseType)
            .CreateType();
    }
}
