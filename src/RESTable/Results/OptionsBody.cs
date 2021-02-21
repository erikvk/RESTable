using System.Collections.Generic;
using RESTable.Meta;

namespace RESTable.Results
{
    internal class OptionsBody
    {
        public string Resource { get; }
        public ResourceKind ResourceKind { get; }
        public IEnumerable<Method> Methods { get; }

        public OptionsBody(string resource, ResourceKind resourceKind, IEnumerable<Method> methods)
        {
            Resource = resource;
            ResourceKind = resourceKind;
            Methods = methods;
        }
    }
}