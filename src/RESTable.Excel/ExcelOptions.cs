using System.Text;

namespace RESTable.Excel
{
    public class ExcelOptions
    {
        public Encoding Encoding { get; set; }

        public ExcelOptions()
        {
            Encoding = new UTF8Encoding(false);
        }
    }
}