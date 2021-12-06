using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RESTable.Auth;
using RESTable.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;

namespace RESTable.OData;

/// <inheritdoc />
/// <summary>
///     This class is a low-level implementation of an OData metadata writer, that writes an XML
///     metadata document from a RESTable metadata object. It's pretty bare-boned in the current
///     implementation, but it does the trick.
/// </summary>
[RESTable(GETAvailableToAll = true, Description = description)]
public class MetadataDocument : IBinary<MetadataDocument>
{
    private const string description = "The OData metadata document, defining the metadata for the " +
                                       "resources of this application";

    private static readonly ContentType ContentType = "application/xml; charset=utf-8";

    /// <inheritdoc />
    public BinaryResult Select(IRequest<MetadataDocument> request)
    {
        var rootAccess = request.GetRequiredService<RootAccess>();
        var typeCache = request.GetRequiredService<TypeCache>();

        async Task WriteStream(Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = Metadata.GetMetadata(MetadataLevel.Full, rootAccess, typeCache);

            var swr = new StreamWriter(stream, Encoding.UTF8, 4096, true);
#if NETSTANDARD2_0
            using (swr)
#else
            await using (swr.ConfigureAwait(false))
#endif
            {
                await swr.WriteAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>").ConfigureAwait(false);
                await swr.WriteAsync("<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices>").ConfigureAwait(false);
                await swr.WriteAsync("<Schema Namespace=\"global\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">").ConfigureAwait(false);

                var (enumTypes, complexTypes) = metadata.PeripheralTypes.Split(t => t.Key.IsEnum);

                #region Print enum types

                foreach (var (key, _) in enumTypes)
                {
                    await swr.WriteAsync($"<EnumType Name=\"{key.FullName}\">").ConfigureAwait(false);
                    foreach (var member in EnumMember.GetMembers(key))
                        await swr.WriteAsync($"<Member Name=\"{member.Name}\" Value=\"{member.NumericValue}\"/>").ConfigureAwait(false);
                    await swr.WriteAsync("</EnumType>").ConfigureAwait(false);
                }

                #endregion

                #region Print complex types

                foreach (var (type, members) in complexTypes)
                {
                    var (dynamicMembers, declaredMembers) = members.Split(IsDynamicMember);
                    var isOpenType = type.IsDictionary(out _, out _) || dynamicMembers.Any();
                    await swr.WriteAsync($"<ComplexType Name=\"{type.FullName}\" OpenType=\"{isOpenType.XMLBool()}\">").ConfigureAwait(false);
                    await WriteMembers(swr, declaredMembers).ConfigureAwait(false);
                    await swr.WriteAsync("</ComplexType>").ConfigureAwait(false);
                }

                #endregion

                #region Print entity types

                foreach (var (type, members) in metadata.EntityResourceTypes.Where(t => t.Key != typeof(Metadata)))
                {
                    var (dynamicMembers, declaredMembers) = members.Split(IsDynamicMember);
                    var isOpenType = type.IsDictionary(out _, out _) || dynamicMembers.Any();
                    await swr.WriteAsync($"<EntityType Name=\"{type.FullName}\" OpenType=\"{isOpenType.XMLBool()}\">").ConfigureAwait(false);
                    var key = declaredMembers.OfType<DeclaredProperty>().FirstOrDefault(p => p.HasAttribute<KeyAttribute>());
                    if (key is not null)
                        await swr.WriteAsync($"<Key><PropertyRef Name=\"{key.Name}\"/></Key>").ConfigureAwait(false);
                    await WriteMembers(swr, declaredMembers.Where(p => p is not DeclaredProperty {Hidden: true} d || d.Equals(key))).ConfigureAwait(false);
                    await swr.WriteAsync("</EntityType>").ConfigureAwait(false);
                }
                await swr.WriteAsync("<EntityType Name=\"RESTable.DynamicResource\" OpenType=\"true\"/>").ConfigureAwait(false);

                #endregion

                #region Write entity container and entity sets

                await swr.WriteAsync($"<EntityContainer Name=\"{EntityContainerName}\">").ConfigureAwait(false);
                foreach (var entitySet in metadata.EntityResources.Where(t => t.Type != typeof(Metadata)))
                {
                    await swr.WriteAsync($"<EntitySet EntityType=\"{GetEdmTypeName(entitySet.Type)}\" Name=\"{entitySet.Name}\">").ConfigureAwait(false);
                    var methods = metadata.CurrentAccessScope[entitySet].Intersect(entitySet.AvailableMethods).ToList();
                    await swr.WriteAsync(InsertableAnnotation(methods.Contains(Method.POST))).ConfigureAwait(false);
                    await swr.WriteAsync(UpdatableAnnotation(methods.Contains(Method.PATCH))).ConfigureAwait(false);
                    await swr.WriteAsync(DeletableAnnotation(methods.Contains(Method.DELETE))).ConfigureAwait(false);
                    await swr.WriteAsync("</EntitySet>").ConfigureAwait(false);
                }
                await swr.WriteAsync("</EntityContainer>").ConfigureAwait(false);
                await swr.WriteAsync($"<Annotations Target=\"global.{EntityContainerName}\">").ConfigureAwait(false);
                await swr.WriteAsync("<Annotation Term=\"Org.OData.Capabilities.V1.ConformanceLevel\"><EnumMember>Org.OData.Capabilities.V1." +
                                     "ConformanceLevelType/Minimal</EnumMember></Annotation>").ConfigureAwait(false);
                await swr.WriteAsync("<Annotation Term=\"Org.OData.Capabilities.V1.SupportedFormats\">").ConfigureAwait(false);
                await swr.WriteAsync("<Collection>").ConfigureAwait(false);
                await swr.WriteAsync("<String>application/json;odata.metadata=minimal;IEEE754Compatible=false;odata.streaming=true</String>").ConfigureAwait(false);
                await swr.WriteAsync("</Collection>").ConfigureAwait(false);
                await swr.WriteAsync("</Annotation>").ConfigureAwait(false);
                await swr.WriteAsync("<Annotation Bool=\"true\" Term=\"Org.OData.Capabilities.V1.AsynchronousRequestsSupported\"/>").ConfigureAwait(false);
                await swr.WriteAsync("<Annotation Term=\"Org.OData.Capabilities.V1.FilterFunctions\"><Collection></Collection></Annotation>").ConfigureAwait(false);
                await swr.WriteAsync("</Annotations>").ConfigureAwait(false);
                await swr.WriteAsync("</Schema>").ConfigureAwait(false);
                await swr.WriteAsync("</edmx:DataServices></edmx:Edmx>").ConfigureAwait(false);

                #endregion
            }
        }

        return new BinaryResult(WriteStream, ContentType);
    }

    private static async Task WriteMembers(TextWriter swr, IEnumerable<Member> members)
    {
        foreach (var member in members)
        {
            await swr.WriteAsync($"<Property Name=\"{member.Name}\" Nullable=\"{member.IsNullable.XMLBool()}\" " +
                                 $"Type=\"{GetEdmTypeName(member.Type)}\" ").ConfigureAwait(false);
            if (member.IsReadOnly)
                await swr.WriteAsync($">{ReadOnlyAnnotation}</Property>").ConfigureAwait(false);
            else if (member.IsWriteOnly)
                await swr.WriteAsync($">{WriteOnlyAnnotation}</Property>").ConfigureAwait(false);
            else await swr.WriteAsync("/>").ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Gets an Edm type name from a .NET type
    /// </summary>
    private static string GetEdmTypeName(Type type)
    {
        if (type.IsEnum) return "global." + type.FullName;
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Object => type switch
            {
                _ when type == typeof(Guid) => "Edm.Guid",
                _ when type.IsNullable(out var baseType) => GetEdmTypeName(baseType!),
                _ when type.ImplementsGenericInterface(typeof(IDictionary<,>), out var p) && p![0] == typeof(string) => "global.RESTable.DynamicResource",
                _ when type.ImplementsGenericInterface(typeof(IEnumerable<>), out var p) => $"Collection({GetEdmTypeName(p![0])})",
                _ => $"global.{type.FullName}"
            },
            TypeCode.Boolean => "Edm.Boolean",
            TypeCode.Byte => "Edm.Byte",
            TypeCode.DateTime => "Edm.DateTimeOffset",
            TypeCode.Decimal => "Edm.Decimal",
            TypeCode.Double => "Edm.Double",
            TypeCode.Single => "Edm.Single",
            TypeCode.Int16 => "Edm.Int16",
            TypeCode.UInt16 => "Edm.Int32",
            TypeCode.Int32 => "Edm.Int32",
            TypeCode.UInt32 => "Edm.Int64",
            TypeCode.UInt64 => "Edm.Int64",
            TypeCode.Int64 => "Edm.Int64",
            TypeCode.SByte => "Edm.SByte",
            TypeCode.Char => "Edm.String",
            TypeCode.String => "Edm.String",
            _ => $"global.{type.FullName}"
        };
    }

    /// <summary>
    ///     We have to know whether the member is of dynamic type. If it is, the type has to be
    ///     declared as open.
    /// </summary>
    private static bool IsDynamicMember(Member member)
    {
        return member.Type == typeof(object);
    }

    #region Annotations

    /// <summary>
    ///     This annotation marks a property as read-only
    /// </summary>
    private const string ReadOnlyAnnotation = "<Annotation Term=\"Org.OData.Core.V1.Permissions\">" +
                                              "<EnumMember>Org.OData.Core.V1.Permission/Read</EnumMember>" +
                                              "</Annotation>";

    /// <summary>
    ///     This annotation marks a property as write-only
    /// </summary>
    private const string WriteOnlyAnnotation = "<Annotation Term=\"Org.OData.Core.V1.Permissions\">" +
                                               "<EnumMember>Org.OData.Core.V1.Permission/Write</EnumMember>" +
                                               "</Annotation>";


    /// <summary>
    ///     This annotation is used to print XML describing whether the entity set support inserts
    /// </summary>
    private static string InsertableAnnotation(bool state)
    {
        return "<Annotation Term=\"Org.OData.Capabilities.V1.InsertRestrictions\">" +
               $"<Record><PropertyValue Bool=\"{state.XMLBool()}\" Property=\"Insertable\"/>" +
               "<PropertyValue Property=\"NonInsertableNavigationProperties\">" +
               "<Collection/></PropertyValue></Record></Annotation>";
    }

    /// <summary>
    ///     This annotation is used to print XML describing whether the entity set support updates
    /// </summary>
    private static string UpdatableAnnotation(bool state)
    {
        return "<Annotation Term=\"Org.OData.Capabilities.V1.UpdateRestrictions\">" +
               $"<Record><PropertyValue Bool=\"{state.XMLBool()}\" Property=\"Updatable\"/>" +
               "<PropertyValue Property=\"NonUpdatableNavigationProperties\">" +
               "<Collection/></PropertyValue></Record></Annotation>";
    }

    /// <summary>
    ///     This annotation is used to print XML describing whether the entity set support deletions
    /// </summary>
    private static string DeletableAnnotation(bool state)
    {
        return "<Annotation Term=\"Org.OData.Capabilities.V1.DeleteRestrictions\">" +
               $"<Record><PropertyValue Bool=\"{state.XMLBool()}\" Property=\"Deletable\"/>" +
               "<PropertyValue Property=\"NonDeletableNavigationProperties\">" +
               "<Collection/></PropertyValue></Record></Annotation>";
    }

    /// <summary>
    ///     The name of the entitycontainer
    /// </summary>
    private const string EntityContainerName = "DefaultContainer";

    #endregion
}