using System.Text;

namespace RESTable.Excel;

public class ExcelOptions
{
    public ExcelOptions()
    {
        Encoding = new UTF8Encoding(false);
    }

    public Encoding Encoding { get; set; }
}
