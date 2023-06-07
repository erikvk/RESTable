using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Returned to the client on successful REPORT requests
/// </summary>
public class Report : Content
{
    public Report(IRequest request, long count) : base(request)
    {
        Count = count;
    }

    public long Count { get; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public override string Metadata => $"{nameof(Report)};{Request.Resource};{Count}";
}
