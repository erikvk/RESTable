using System;
using System.IO;
using Newtonsoft.Json;

#pragma warning disable 1591

namespace RESTable.Excel
{
    internal class RESTableFromExcelJsonTextWriter : JsonTextWriter
    {
        public RESTableFromExcelJsonTextWriter(TextWriter textWriter) : base(textWriter) { }

        public override void WriteValue(double value)
        {
            if (Math.Abs(value % 1) <= double.Epsilon * 100)
                WriteValue((int) value);
            else base.WriteValue(value);
        }
    }
}