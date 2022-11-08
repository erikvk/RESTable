using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.Results;

/// <inheritdoc />
/// <summary>
///     Holds a binary content, used as result from requests to binary resources
/// </summary>
public sealed class Binary : Content
{
    /// <inheritdoc />
    public Binary(IRequest request, BinaryResult binaryResult) : base(request, binaryResult.ContentType)
    {
        BinaryResult = binaryResult;
        if (binaryResult.ContentLength is long contentLength) Headers["Content-Length"] = contentLength.ToString();
        if (binaryResult.ContentDisposition is string contentDisposition) Headers["Content-Disposition"] = contentDisposition;
    }

    [RESTableMember(true)] public BinaryResult BinaryResult { get; }

    /// <inheritdoc />
    [RESTableMember(true)]
    public override string Metadata => $"{nameof(Binary)};{Request.Resource};";
}
