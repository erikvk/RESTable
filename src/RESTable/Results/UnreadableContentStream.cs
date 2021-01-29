namespace RESTable.Results
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters a content stream that is unreadable
    /// </summary>
    internal class UnreadableContentStream : Internal
    {
        internal UnreadableContentStream(Content content) : base(ErrorCodes.UnreadableContentStream,
            $"RESTable encountered an unreadable content stream from resource '{content.Request.Resource}'") { }
    }
}