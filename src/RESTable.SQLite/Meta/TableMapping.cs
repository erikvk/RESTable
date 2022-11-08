using System;
using System.Collections.Generic;

namespace RESTable.Sqlite.Meta;

/// <summary>
///     A static class for accessing table mappings
/// </summary>
/// <typeparam name="T"></typeparam>
public static class TableMapping<T> where T : SqliteTable
{
    private static TableMapping? mapping;

    /// <summary>
    ///     Gets the table mapping for the given type
    /// </summary>
    public static TableMapping Get => mapping ??= TableMapping.GetTableMapping(typeof(T)) ??
                                                  throw new Exception($"No table mapping for type '{typeof(T).GetRESTableTypeName()}'");

    /// <summary>
    ///     Gets the name from the table mapping for the given type
    /// </summary>
    public static string TableName => Get.TableName;

    /// <summary>
    ///     Gets the column mappings from the table mapping for the given type
    /// </summary>
    public static ColumnMappings ColumnMappings => Get.ColumnMappings;

    internal static (string name, string columns, string[] param, ColumnMapping[] mappings) InsertSpec => Get.InsertSpec;
    internal static (string name, string set, string[] param, ColumnMapping[] mappings) UpdateSpec => Get.UpdateSpec;

    internal static ColumnMapping[] TransactMappings => Get.TransactMappings;

    /// <summary>
    ///     Gets the column names from the table mapping for the given type
    /// </summary>
    internal static HashSet<string> SqlColumnNames => Get.SqlColumnNames;
}
