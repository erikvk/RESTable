using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using Starcounter;
using static RESTar.Operators;
using static RESTar.RESTarPresets;

namespace RESTar
{
    [RESTar(ReadOnly, Dynamic = true)]
    public class TableInfo : Dictionary<string, dynamic>, ISelector<TableInfo>
    {
        public string Resource { get; set; }

        public IEnumerable<TableInfo> Select(IRequest request)
        {
            var input = (string) request.Conditions[nameof(Resource), EQUALS];
            if (input == null) throw new Exception("No resource specified");
            var resource = input.FindResource();
            if (!resource.IsStarcounterResource)
                throw new Exception($"'{resource.Name}' is not a Starcounter resource, and has no table info");

            var domainCount = DB.RowCount(resource.Name);
            var props = resource.GetStaticProperties();
            IEnumerable<dynamic> extension = Db.SQL($"SELECT t FROM {resource.Name} t");
            var totalBytes = 0L;

            if (domainCount <= 1000)
                extension.ForEach(e => props.ForEach(p => totalBytes += p.ByteCount(e)));
            else
            {
                var step = domainCount / 1000;
                var sample = extension.Where((_, i) => i % step == 0).ToList();
                var sampleRate = (decimal) sample.Count / domainCount;
                var sampleBytes = 0L;
                sample.ForEach(e => props.ForEach(p => sampleBytes += p.ByteCount(e)));
                var total = sampleBytes / sampleRate;
                totalBytes = decimal.ToInt64(total ?? 0M);
            }

            return new[]
            {
                new TableInfo
                {
                    ["Table name"] = resource.Name,
                    ["Number of rows"] = domainCount,
                    ["Number of columns"] = props.Count(),
                    ["Table size"] = new
                    {
                        GB = decimal.Round((decimal) totalBytes/1000000000, 6),
                        MB = decimal.Round((decimal) totalBytes/1000000, 6),
                        KB = decimal.Round((decimal) totalBytes/1000, 6),
                        Bytes = totalBytes
                    }
                }
            };
        }
    }
}