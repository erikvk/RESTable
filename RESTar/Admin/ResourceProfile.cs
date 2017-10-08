using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Deflection.Dynamic;
using RESTar.Linq;
using Starcounter;
using Starcounter.Metadata;
using IResource = RESTar.Internal.IResource;

namespace RESTar.Admin
{
    /// <summary>
    /// Provides a profile for a given resource
    /// </summary>
    [RESTar(Methods.GET, Description = description)]
    public class ResourceProfile : ISelector<ResourceProfile>
    {
        private const int baseObjectBytes = 16;

        private const string description = "The TableInfo resource can create aggregated " +
                                           "info views for Starcounter tables.";

        /// <summary>
        /// The name of the resource
        /// </summary>
        public string Resource { get; set; }

        /// <summary>
        /// The number of entities in the resource
        /// </summary>
        public long NumberOfEntities { get; set; }

        /// <summary>
        /// An approximation of the resource size in memory
        /// </summary>
        public ResourceSize ApproximateSize { get; set; }

        /// <inheritdoc />
        public IEnumerable<ResourceProfile> Select(IRequest<ResourceProfile> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            IEnumerable<IResource> resources;
            string input = request.Conditions.Get(nameof(Resource), Operators.EQUALS)?.Value;
            if (input == null)
                resources = RESTarConfig.Resources.Where(r => r.IsStarcounterResource);
            else
            {
                var resource = RESTar.Resource.Find(input);
                if (!resource.IsStarcounterResource)
                    throw new Exception($"'{resource.Name}' is not a Starcounter resource, and has no table info");
                resources = new[] {resource};
            }
            return resources
                .Select(r => Make(r.Type))
                .Where(request.Conditions)
                .OrderByDescending(t => t.ApproximateSize.Bytes)
                .ToList();
        }

        internal static ResourceProfile MakeStarcounter(Type starcounter)
        {
            const string columnSQL = "SELECT t FROM Starcounter.Metadata.Column t WHERE t.Table.Fullname =?";
            var resourceSQLName = starcounter.FullName;
            var columns = Db.SQL<Column>(columnSQL, resourceSQLName).Select(c => c.Name).ToList();
            var domainCount = Db.SQL<long>($"SELECT COUNT(t) FROM {resourceSQLName.Fnuttify()} t").First;
            var properties = starcounter.GetTableColumns().Where(p => columns.Contains(p.DatabaseQueryName)).ToList();
            var scExtension = Db.SQL($"SELECT t FROM {resourceSQLName.Fnuttify()} t");
            var totalBytes = 0L;
            if (domainCount <= 1000)
                scExtension.ForEach(e => totalBytes += properties.Sum(p => p.ByteCount(e)) + baseObjectBytes);
            else
            {
                var step = domainCount / 1000;
                var sample = scExtension.Where((_, i) => i % step == 0).ToList();
                var sampleRate = (decimal) sample.Count / domainCount;
                var sampleBytes = 0L;
                sample.ForEach(e => sampleBytes += properties.Sum(p => p.ByteCount(e)) + baseObjectBytes);
                var total = sampleBytes / sampleRate;
                totalBytes = decimal.ToInt64(total);
            }
            return new ResourceProfile
            {
                Resource = starcounter.FullName,
                NumberOfEntities = domainCount,
                ApproximateSize = new ResourceSize(totalBytes)
            };
        }

        internal static ResourceProfile MakeDDictionary(Type ddict)
        {
            var sqlName = ddict.FullName.Fnuttify();
            var domainCount = Db.SQL<long>($"SELECT COUNT(t) FROM {sqlName} t").First;
            var ddictExtension = Db.SQL<DDictionary>($"SELECT t FROM {sqlName} t");
            long totalBytes;
            if (domainCount <= 1000)
                totalBytes = ddictExtension.Sum(entity => entity.KeyValuePairs.Sum(kvp => kvp.ByteCount) + baseObjectBytes);
            else
            {
                var step = domainCount / 1000;
                var sample = ddictExtension.Where((_, i) => i % step == 0).ToList();
                var sampleRate = (decimal) sample.Count / domainCount;
                var sampleBytes = ddictExtension.Sum(entity => entity.KeyValuePairs.Sum(kvp => kvp.ByteCount) + baseObjectBytes);
                var total = sampleBytes / sampleRate;
                totalBytes = decimal.ToInt64(total);
            }
            return new ResourceProfile
            {
                Resource = ddict.FullName,
                NumberOfEntities = domainCount,
                ApproximateSize = new ResourceSize(totalBytes)
            };
        }

        /// <summary>
        /// Creates a ResourceProfile for the given type.
        /// </summary>
        /// <param name="type">The type to profile</param>
        public static ResourceProfile Make(Type type)
        {
            switch (type)
            {
                case var _ when type.IsDDictionary(): return MakeDDictionary(type);
                case var _ when type.IsStarcounter(): return MakeStarcounter(type);
                default:
                    return RESTar.Resource.SafeGet(type)?.ResourceProfile
                           ?? throw new ArgumentException($"Cannot profile '{type.FullName}'. No profiler implemented for type");
            }
        }
    }

    /// <summary>
    /// Contains a description of a resource size in memory
    /// </summary>
    public struct ResourceSize
    {
        /// <summary>
        /// The size in bytes
        /// </summary>
        public readonly long Bytes;

        /// <summary>
        /// The size in kilobytes
        /// </summary>
        public readonly decimal KB;

        /// <summary>
        /// The size in megabytes
        /// </summary>
        public readonly decimal MB;

        /// <summary>
        /// The size in gigabytes
        /// </summary>
        public readonly decimal GB;

        /// <summary>
        /// Creates a new ResourceSize instance, encoding the given bytes
        /// </summary>
        public ResourceSize(long bytes)
        {
            Bytes = bytes;
            var decimalBytes = (decimal) bytes;
            GB = decimal.Round(decimalBytes / 1_000_000_000, 6);
            MB = decimal.Round(decimalBytes / 1_000_000, 6);
            KB = decimal.Round(decimalBytes / 1_000, 6);
        }
    }
}