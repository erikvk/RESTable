using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using Starcounter;
using static RESTar.Operators;
using static RESTar.RESTarPresets;
using Starcounter.Metadata;

namespace RESTar
{
    public class TableSize
    {
        public long Bytes { get; set; }
        public decimal KB { get; set; }
        public decimal MB { get; set; }
        public decimal GB { get; set; }
    }

    [RESTar(ReadOnly, Dynamic = true)]
    public class TableInfo : ISelector<TableInfo>
    {
        public string TableName { get; set; }
        public long NumberOfRows { get; set; }
        public int NumberOfColumns { get; set; }
        public TableSize ApproximateTableSize { get; set; }

        public IEnumerable<TableInfo> Select(IRequest request)
        {
            IEnumerable<Internal.IResource> resources;
            var input = (string) request.Conditions?[nameof(TableName), EQUALS];
            if (input == null)
                resources = RESTarConfig.Resources.Where(r => r.IsStarcounterResource);
            else
            {
                var resource = input.FindResource();
                if (!resource.IsStarcounterResource)
                    throw new Exception($"'{resource.Name}' is not a Starcounter resource, and has no table info");
                resources = new[] {resource};
            }
            return resources.Select(GetTableInfo).ToList();
        }

        private static IEnumerable<Column> GetColumns(string resourceName) => Db.SQL<Column>(
            $"SELECT t FROM {typeof(Column).FullName} t WHERE t.Table.Fullname =?", resourceName);

        internal static TableInfo GetTableInfo(Internal.IResource resource)
        {
            var columns = GetColumns(resource.Name).Select(c => c.Name);
            var domainCount = DB.RowCount(resource.Name);
            var properties = resource.GetTableColumns().Where(p => columns.Contains(p.DatabaseQueryName)).ToList();
            IEnumerable<dynamic> extension = Db.SQL($"SELECT t FROM {resource.Name} t");
            var totalBytes = 0L;
            const int addBytes = 16;

            if (domainCount <= 1000)
                extension.ForEach(e =>
                {
                    foreach (var p in properties)
                        totalBytes += p.ByteCount(e);
                    totalBytes += addBytes;
                });
            else
            {
                var step = domainCount / 1000;
                var sample = extension.Where((_, i) => i % step == 0).ToList();
                var sampleRate = (decimal) sample.Count / domainCount;
                var sampleBytes = 0L;
                sample.ForEach(e => 
                {
                    foreach (var p in properties)
                        sampleBytes += p.ByteCount(e);
                    sampleBytes += addBytes;
                });
                var total = sampleBytes / sampleRate;
                totalBytes = decimal.ToInt64(total ?? 0M);
            }

            return new TableInfo
            {
                TableName = resource.Name,
                NumberOfRows = domainCount.GetValueOrDefault(),
                NumberOfColumns = properties.Count(),
                ApproximateTableSize = new TableSize
                {
                    GB = decimal.Round((decimal) totalBytes / 1000000000, 6),
                    MB = decimal.Round((decimal) totalBytes / 1000000, 6),
                    KB = decimal.Round((decimal) totalBytes / 1000, 6),
                    Bytes = totalBytes
                }
            };
        }
    }
}