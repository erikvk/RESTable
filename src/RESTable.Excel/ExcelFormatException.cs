using System;

namespace RESTable.Excel;

/// <inheritdoc />
/// <summary>
///     Thrown when RESTable encounters an error when writing to Excel
/// </summary>
public class ExcelFormatException : Exception
{
    internal ExcelFormatException(string message, Exception ie) : base($"RESTable was unable to write entities to excel. {message}. ", ie) { }
}
