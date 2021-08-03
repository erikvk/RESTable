using System;
using System.Linq;
using RESTable.Meta;

namespace RESTable
{
    public class RESTableConfiguration
    {
        public Method[] Methods { get; }
        public string Version { get; }
        public string[] ReservedNamespaces { get; }
        public string RootUri { get; internal set; }

        public RESTableConfiguration()
        {
            RootUri = null!;
            Methods = EnumMember<Method>.Values;
            var version = typeof(RESTableConfigurator).Assembly.GetName().Version
                          ?? throw new Exception("Could not establish version for the current RESTable assembly");
            Version = $"{version.Major}.{version.Minor}.{version.Build}";
            ReservedNamespaces = typeof(RESTableConfigurator).Assembly
                .GetTypes()
                .Select(type => type.Namespace?.ToLower())
                .Where(ns => ns is not null)
                .Distinct()
                .ToArray()!;
        }
    }
}