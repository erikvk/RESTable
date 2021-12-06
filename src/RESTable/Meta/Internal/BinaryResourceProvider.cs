using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Resources.Operations;

namespace RESTable.Meta.Internal;

public class BinaryResourceProvider
{
    public BinaryResourceProvider(TypeCache typeCache, ResourceCollection resourceCollection)
    {
        BuildBinaryMethod = typeof(BinaryResourceProvider).GetMethod(nameof(MakeBinaryResource), BindingFlags.Instance | BindingFlags.NonPublic)!;
        BuildAsyncBinaryMethod = typeof(BinaryResourceProvider).GetMethod(nameof(MakeAsyncBinaryResource), BindingFlags.Instance | BindingFlags.NonPublic)!;
        TypeCache = typeCache;
        ResourceCollection = resourceCollection;
    }

    private MethodInfo BuildBinaryMethod { get; }
    private MethodInfo BuildAsyncBinaryMethod { get; }
    private TypeCache TypeCache { get; }
    private ResourceCollection ResourceCollection { get; }

    internal void RegisterBinaryTypes(IEnumerable<Type> binaryTypes)
    {
        foreach (var type in binaryTypes.OrderBy(t => t.GetRESTableTypeName()))
            if (type.ImplementsGenericInterface(typeof(IAsyncBinary<>)))
            {
                var resource = (IResource?) BuildAsyncBinaryMethod.MakeGenericMethod(type).Invoke(this, null);
                if (resource is not null)
                    ResourceCollection.AddResource(resource);
            }
            else
            {
                var resource = (IResource?) BuildBinaryMethod.MakeGenericMethod(type).Invoke(this, null);
                if (resource is not null)
                    ResourceCollection.AddResource(resource);
            }
    }

    private IResource MakeBinaryResource<T>() where T : class, IBinary<T>
    {
        var binarySelector = DelegateMaker.GetDelegate<BinarySelector<T>>(typeof(T));

        ValueTask<BinaryResult> async(IRequest<T> request, CancellationToken token)
        {
            return new(binarySelector!(request));
        }

        return new BinaryResource<T>(async, TypeCache);
    }

    private IResource MakeAsyncBinaryResource<T>() where T : class, IAsyncBinary<T>
    {
        var binarySelector = DelegateMaker.GetDelegate<AsyncBinarySelector<T>>(typeof(T));
        return new BinaryResource<T>(binarySelector!, TypeCache);
    }
}