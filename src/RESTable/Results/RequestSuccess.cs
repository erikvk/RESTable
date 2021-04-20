using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results
{
    /// <inheritdoc cref="ISerializedResult" />
    /// <inheritdoc cref="IResult" />
    /// <summary>
    /// The successful result of a RESTable request operation
    /// </summary>
    public abstract class RequestSuccess : Success
    {
        [RESTableMember(ignore: true)]
        public sealed override IRequest Request { get; }

        internal RequestSuccess(IRequest request) : base(request)
        {
            Request = request;
        }
    }
}