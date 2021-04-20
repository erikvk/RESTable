using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Returned to the client on successful REPORT requests
    /// </summary>
    public class Report : Content
    {
        public ulong Count { get; }

        public Report(IRequest request, ulong count) : base(request)
        {
            Count = count;
        }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public override string Metadata => $"{nameof(Report)};{Request.Resource};{Count}";
    }
}