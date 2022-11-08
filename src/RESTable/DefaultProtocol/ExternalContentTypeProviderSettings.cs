namespace RESTable.DefaultProtocol;

/// <summary>
///     Enumeration used to configure how a RESTable protocol provider works with external
///     content type providers.
/// </summary>
public enum ExternalContentTypeProviderSettings
{
    /// <summary>
    ///     Allow all external content type providers
    /// </summary>
    AllowAll,

    /// <summary>
    ///     Allow external content type providers only when deserializing request bodies
    /// </summary>
    AllowInput,

    /// <summary>
    ///     Allow external content type providers only when serializing response bodies
    /// </summary>
    AllowOutput,

    /// <summary>
    ///     Do not allow any external content type providers
    /// </summary>
    DontAllow
}
