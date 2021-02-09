using RESTable.Requests;

namespace RESTable.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The successful result of a RESTable request operation
    /// </summary>
    public abstract class RequestSuccess : Success
    {
        public override IRequest Request { get; }

        internal RequestSuccess(IRequest request) : base(request)
        {
            Request = request;
            TimeElapsed = request.TimeElapsed;
        }
    }
}