using System;
using RESTable.Resources;

namespace RESTable.Meta.Internal
{
    internal class VirtualResourceProvider : EntityResourceProvider<object>
    {
        protected override bool Include(Type type) => !type.HasResourceProviderAttribute();
        protected override void Validate() { }
        protected override Type AttributeType { get; } = null;
    }
}