using RESTar.Internal;

namespace RESTar.Results.Error.BadRequest
{
    /// <inheritdoc />
    public class MissingDataSource : BadRequest
    {
        /// <inheritdoc />
        public MissingDataSource(IQuery query) : base(ErrorCodes.NoDataSource, $"Missing data source for method {query.Method.ToString()}") { }
    }
}