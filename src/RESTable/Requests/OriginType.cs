namespace RESTable.Requests;

/// <summary>
///     Describes the origin type of a request
/// </summary>
public enum OriginType
{
    /// <summary>
    ///     The request originated from within the RESTable application
    /// </summary>
    Internal,

    /// <summary>
    ///     The request originated from outside the RESTable application
    /// </summary>
    External
}