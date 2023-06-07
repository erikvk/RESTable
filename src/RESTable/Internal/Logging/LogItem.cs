using System;
using RESTable.Requests;

namespace RESTable.Internal.Logging;

internal struct LogItem
{
    public string Type;
    public string Id;
    public string? Message;
    public ClientInfo? Client;
    public string? Content;
    public Headers CustomHeaders;
    public DateTime? Time;
}
