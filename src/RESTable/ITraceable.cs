using RESTable.Requests;

namespace RESTable;

/// <summary>
///     Defines something that can be traced back to an initial message
/// </summary>
public interface ITraceable
{
    /// <summary>
    ///     The context to which this trace can be led
    /// </summary>
    RESTableContext Context { get; }
}