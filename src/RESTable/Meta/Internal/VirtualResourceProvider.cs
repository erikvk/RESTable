using System;
using RESTable.Resources;

namespace RESTable.Meta.Internal;

public class VirtualResourceProvider : EntityResourceProvider<object>
{
    protected override Type AttributeType => null!;

    protected override bool Include(Type type)
    {
        return !type.HasResourceProviderAttribute();
    }

    protected override void Validate() { }
}