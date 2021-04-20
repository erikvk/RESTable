namespace RESTable.Requests
{
    /// <inheritdoc cref="IUriComponents" />
    /// <summary>
    /// Defines the operations of a RESTable macro
    /// </summary>
    public interface IMacro : IUriComponents
    {
        /// <summary>
        /// The name of the macro
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Does this macro contain a body?
        /// </summary>
        bool HasBody { get; }

        /// <summary>
        /// The body of the macro, as byte array
        /// </summary>
        Body Body { get; }

        /// <summary>
        /// The content type of the body of the macro
        /// </summary>
        ContentType ContentType { get; }

        /// <summary>
        /// The headers of the macro
        /// </summary>
        IHeaders Headers { get; }

        /// <summary>
        /// Should the macro overwrite the body of the calling request?
        /// </summary>
        bool OverwriteBody { get; }

        /// <summary>
        /// Should the macro overwrite matching headers in the calling request?
        /// </summary>
        bool OverwriteHeaders { get; }
    }
}