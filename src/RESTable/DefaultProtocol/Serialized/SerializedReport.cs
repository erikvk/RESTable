using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized;

public class SerializedReport : ISerialized
{
    public SerializedReport(Report report)
    {
        Report = report;
    }

    private Report Report { get; }
    public long Count => Report.Count;
    public double TimeElapsedMs => Report.TimeElapsed.GetRESTableElapsedMs();

    public string Status => "success";
}