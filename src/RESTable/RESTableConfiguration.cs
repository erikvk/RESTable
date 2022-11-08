using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RESTable.Resources;

namespace RESTable;

public class RESTableConfiguration
{
    public const string ConfigSection = "RESTable";
    private int maxNumberOfEntitiesInChangeResults = 100;
    private bool maxNumberOfEntitiesInChangeResultsSet;

    // Defaults:

    private int numberOfErrorsToKeep = 2000;

    // Flags for setters (we can't set properties from the app post bind):

    private bool numberOfErrorsToKeepSet;
    private int webSocketBufferSize = 4096;
    private bool webSocketBufferSizeSet;

    // Read-only delegating calls to statics:

    public string Version => _Version;

    [RESTableMember(hide: true)] public IReadOnlyList<string> ReservedNamespaces => _ReservedNamespaces;

    public string RunningExecutablePath => _RunningExecutablePath;

    /// <summary>
    ///     The path where temporary files are created
    /// </summary>
    [RESTableMember(hide: true)]
    public string TempFilePath => _TempFilePath;

    // Option members:

    public int NumberOfErrorsToKeep
    {
        get => numberOfErrorsToKeep;
        set
        {
            if (numberOfErrorsToKeepSet)
                return;
            numberOfErrorsToKeep = value;
            numberOfErrorsToKeepSet = true;
        }
    }

    public int WebSocketBufferSize
    {
        get => webSocketBufferSize;
        set
        {
            if (webSocketBufferSizeSet)
                return;
            webSocketBufferSize = value;
            webSocketBufferSizeSet = true;
        }
    }

    public int MaxNumberOfEntitiesInChangeResults
    {
        get => maxNumberOfEntitiesInChangeResults;
        set
        {
            if (maxNumberOfEntitiesInChangeResultsSet)
                return;
            maxNumberOfEntitiesInChangeResults = value;
            maxNumberOfEntitiesInChangeResultsSet = true;
        }
    }

    #region Statics

    private static string _Version { get; }
    private static IReadOnlyList<string> _ReservedNamespaces { get; }
    private static string _RunningExecutablePath { get; }
    private static string _TempFilePath { get; }

    static RESTableConfiguration()
    {
        _ReservedNamespaces = typeof(RESTableConfiguration).Assembly
            .GetTypes()
            .Select(type => type.Namespace?.ToLower())
            .Where(ns => ns is not null)
            .Distinct()
            .ToList()
            .AsReadOnly()!;
        var version = typeof(RESTableConfiguration).Assembly.GetName().Version
                      ?? throw new Exception("Could not establish version for the current RESTable assembly");
        _Version = $"{version.Major}.{version.Minor}.{version.Build}";
        _RunningExecutablePath = Process.GetCurrentProcess().MainModule?.FileName ?? "Unknown";
        _TempFilePath = Path.GetTempPath();
    }

    #endregion
}
