using System.Collections.Generic;
using System.Linq;
using RESTar.Internal;
using Starcounter;
// ReSharper disable UnassignedGetOnlyAutoProperty
#pragma warning disable 1591

namespace RESTar.Requests
{
    public class ViewRequest<T> : IRequest<T> where T : class
    {
        public RESTarMethods Method { get; }
        public IResource<T> Resource { get; }
        public Conditions Conditions { get; }
        public MetaConditions MetaConditions { get; }
        public string Body { get; }
        public string AuthToken { get; internal set; }
        public IDictionary<string, string> ResponseHeaders { get; }
        public bool Internal { get; }
        IResourceView IRequestView.Resource => Resource;

        internal Request ScRequest { get; }

        public bool ResourceHome => MetaConditions.Empty && Conditions == null;

        internal bool IsSingular(IEnumerable<object> ienum) =>
            Resource.IsSingleton || ienum?.Count() == 1 && !ResourceHome;

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}