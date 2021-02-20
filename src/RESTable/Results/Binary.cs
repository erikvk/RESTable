using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Holds a binary content, used as result from requests to binary resources
    /// </summary>
    public sealed class Binary : Content
    {
        [RESTableMember(ignore: true)]
        public BinaryResult BinaryResult { get; }

        /// <inheritdoc />
        public Binary(IRequest request, BinaryResult binaryResult) : base(request, binaryResult.ContentType)
        {
            BinaryResult = binaryResult;
        }

        /// <inheritdoc />
        [RESTableMember(ignore: true)]
        public override string Metadata => $"{nameof(Binary)};{Request.Resource};";
    }
}