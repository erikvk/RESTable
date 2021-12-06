using System.Net;
using RESTable.Auth;

namespace RESTable.Requests;

/// <summary>
///     The root client, capable of accessing all resources
/// </summary>
public class RootClient : Client
{
    public RootClient(RootAccess rootAccess) : base
    (
        OriginType.Internal,
        "localhost",
        new IPAddress(new byte[] {127, 0, 0, 1}),
        null,
        null,
        false,
        new Cookies(),
        rootAccess
    ) { }
}