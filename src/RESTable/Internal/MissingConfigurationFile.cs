namespace RESTable.Internal
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable requires a configuration file for setting up API keys and/or CORS origins, but
    /// no configuration file was found.
    /// </summary>
    internal class MissingConfigurationFile : RESTableException
    {
        internal MissingConfigurationFile(string info) : base(ErrorCodes.MissingConfigurationFile, info) { }
    }
}