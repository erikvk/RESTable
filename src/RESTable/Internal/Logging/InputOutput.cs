namespace RESTable.Internal.Logging;

internal struct InputOutput
{
    public string Type;
    public ClientInfo? ClientInfo;
    public LogItem In;
    public LogItem Out;
    public double ElapsedMilliseconds;
}
