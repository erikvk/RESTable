using RESTable.Requests;

namespace RESTable;

/// <summary>
///     Defines the operations of an entity that holds headers
/// </summary>
public interface IHeaderHolder : ITraceable
{
    /// <summary>
    ///     The headers of the logable entity
    /// </summary>
    Headers Headers { get; }

    /// <summary>
    ///     A string cache of the headers
    /// </summary>
    string? HeadersStringCache { get; set; }

    /// <summary>
    ///     Should headers be excluded?
    /// </summary>
    bool ExcludeHeaders { get; }
}
