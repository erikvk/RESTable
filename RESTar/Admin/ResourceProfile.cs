using System;
using System.Collections.Generic;
using System.Linq;
using Dynamit;
using RESTar.Internal;
using RESTar.Linq;
using RESTar.Starcounter;
using Starcounter;

namespace RESTar.Admin
{
    /// <inheritdoc />
    /// <summary>
    /// Provides a profile for a given resource
    /// </summary>
    [RESTar(Method.GET, Description = description)]
    public class ResourceProfile : ISelector<ResourceProfile>
    {
        private const int singleSampleCutoff = 1_000;

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

        /// <summary>
        /// The size of the sample used to generate the resource size approximation
        /// </summary>
        public long SampleSize { get; set; }

        /// <inheritdoc />
        public IEnumerable<ResourceProfile> Select(IRequest<ResourceProfile> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            IEnumerable<ResourceProfile> profiles;
            string input = request.Conditions.Get(nameof(Resource), Operators.EQUALS)?.Value;
            request.Conditions.Get(nameof(Resource)).ForEach(c => c.Skip = true);
            if (input == null)
                profiles = RESTarConfig.Resources.OfType<IEntityResource>().Select(r => r.ResourceProfile).Where(r => r != null);
            else
            {
                var resource = RESTar.Resource.Find(input);
                var entityResource = resource as IEntityResource;
                var profile = entityResource?.ResourceProfile
                              ?? throw new Exception($"Cannot profile '{resource.Name}'. No profiler implemented for type");
                profiles = new[] {profile};
            }
            return profiles
                .Where(request.Conditions)
                .OrderByDescending(t => t.ApproximateSize.Bytes)
                .ToList();
        }

        internal static ResourceProfile Make<T>(IEntityResource<T> resource, Func<IEnumerable<T>, long> byteCounter) where T : class
        {
            var sqlName = typeof(T).RESTarTypeName().Fnuttify();
            var domain = SELECT<T>(sqlName);
            var domainCount = COUNT(sqlName);
            long totalBytes, sampleSize;
            if (domainCount <= singleSampleCutoff)
            {
                sampleSize = domainCount;
                totalBytes = byteCounter(domain);
            }
            else
            {
                var step = domainCount / singleSampleCutoff;
                var sample = domain.Where((item, index) => index % step == 0).ToList();
                sampleSize = sample.Count;
                var total = byteCounter(sample) / decimal.Divide(sampleSize, domainCount);
                totalBytes = decimal.ToInt64(total);
            }
            return new ResourceProfile
            {
                Resource = resource.Name,
                NumberOfEntities = domainCount,
                ApproximateSize = new ResourceSize(totalBytes),
                SampleSize = sampleSize
            };
        }

        private static ResourceProfile ScProfiler<T>(IEntityResource<T> r) where T : class => StarcounterOperations<T>.Profile(r);
        private static ResourceProfile DDProfiler<T>(IEntityResource<T> r) where T : DDictionary => DDictionaryOperations<T>.Profile(r);
        private static long COUNT(string name) => Db.SQL<long>($"SELECT COUNT(t) FROM {name} t").FirstOrDefault();
        private static IEnumerable<T> SELECT<T>(string name) => Db.SQL<T>($"SELECT t FROM {name} t");
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