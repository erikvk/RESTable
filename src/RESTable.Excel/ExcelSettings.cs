using System.Text;

namespace RESTable.Excel
{
    public class ExcelSettings
    {
        public Encoding Encoding { get; set; }

        public ExcelSettings()
        {
            Encoding = new UTF8Encoding(false);
        }
    }
}