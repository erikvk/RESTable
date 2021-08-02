using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized
{
    [UseDefaultConverter]
    public class SerializedReport : ISerialized
    {
        private Report Report { get; }

        public string Status => "success";
        public long Count => Report.Count;
        public double TimeElapsedMs => Report.TimeElapsed.GetRESTableElapsedMs();

        public SerializedReport(Report report)
        {
            Report = report;
        }
    }
}