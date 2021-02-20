using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Linq;

namespace RESTable.OData
{
    /// <inheritdoc />
    /// <summary>
    /// This class is a low-level implementation of an OData metadata writer, that writes an XML 
    /// metadata document from a RESTable metadata object. It's pretty bare-boned in the current
    /// implementation, but it does the trick.
    /// </summary>
    [RESTable(GETAvailableToAll = true, Description = description)]
    public class MetadataDocument : IBinary<MetadataDocument>
    {
        private const string description = "The OData metadata document defining the metadata for the " +
                                           "resources of this application";

        #region Annotations

        /// <summary>
        /// This annotation marks a property as read-only
        /// </summary>
        private const string ReadOnlyAnnotation = "<Annotation Term=\"Org.OData.Core.V1.Permissions\">" +
                                                  "<EnumMember>Org.OData.Core.V1.Permission/Read</EnumMember>" +
                                                  "</Annotation>";

        /// <summary>
        /// This annotation marks a property as write-only
        /// </summary>
        private const string WriteOnlyAnnotation = "<Annotation Term=\"Org.OData.Core.V1.Permissions\">" +
                                                   "<EnumMember>Org.OData.Core.V1.Permission/Write</EnumMember>" +
                                                   "</Annotation>";


        /// <summary>
        /// This annotation is used to print XML describing whether the entity set support inserts
        /// </summary>
        private static string InsertableAnnotation(bool state) => "<Annotation Term=\"Org.OData.Capabilities.V1.InsertRestrictions\">" +
                                                                  $"<Record><PropertyValue Bool=\"{state.XMLBool()}\" Property=\"Insertable\"/>" +
                                                                  "<PropertyValue Property=\"NonInsertableNavigationProperties\">" +
                                                                  "<Collection/></PropertyValue></Record></Annotation>";

        /// <summary>
        /// This annotation is used to print XML describing whether the entity set support updates
        /// </summary>
        private static string UpdatableAnnotation(bool state) => "<Annotation Term=\"Org.OData.Capabilities.V1.UpdateRestrictions\">" +
                                                                 $"<Record><PropertyValue Bool=\"{state.XMLBool()}\" Property=\"Updatable\"/>" +
                                                                 "<PropertyValue Property=\"NonUpdatableNavigationProperties\">" +
                                                                 "<Collection/></PropertyValue></Record></Annotation>";

        /// <summary>
        /// This annotation is used to print XML describing whether the entity set support deletions
        /// </summary>
        private static string DeletableAnnotation(bool state) => "<Annotation Term=\"Org.OData.Capabilities.V1.DeleteRestrictions\">" +
                                                                 $"<Record><PropertyValue Bool=\"{state.XMLBool()}\" Property=\"Deletable\"/>" +
                                                                 "<PropertyValue Property=\"NonDeletableNavigationProperties\">" +
                                                                 "<Collection/></PropertyValue></Record></Annotation>";

        /// <summary>
        /// The name of the entitycontainer
        /// </summary>
        private const string EntityContainerName = "DefaultContainer";

        #endregion

        private static readonly ContentType ContentType = "application/xml; charset=utf-8";

        /// <inheritdoc />
        public BinaryResult Select(IRequest<MetadataDocument> request)
        {
            async Task WriteStream(Stream stream)
            {
                var metadata = Metadata.Get(MetadataLevel.Full);
                await using var swr = new StreamWriter(stream, Encoding.UTF8, 1024, true);
                await swr.WriteAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                await swr.WriteAsync("<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices>");
                await swr.WriteAsync("<Schema Namespace=\"global\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">");

                var (enumTypes, complexTypes) = metadata.PeripheralTypes.Split(t => t.Key.IsEnum);

                #region Print enum types

                foreach (var (key, _) in enumTypes)
                {
                    await swr.WriteAsync($"<EnumType Name=\"{key.FullName}\">");
                    foreach (var member in EnumMember.GetMembers(key))
                        await swr.WriteAsync($"<Member Name=\"{member.Name}\" Value=\"{member.NumericValue}\"/>");
                    await swr.WriteAsync("</EnumType>");
                }

                #endregion

                #region Print complex types

                foreach (var (type, members) in complexTypes)
                {
                    var (dynamicMembers, declaredMembers) = members.Split(IsDynamicMember);
                    var isOpenType = type.IsDynamic() || dynamicMembers.Any();
                    await swr.WriteAsync($"<ComplexType Name=\"{type.FullName}\" OpenType=\"{isOpenType.XMLBool()}\">");
                    await WriteMembers(swr, declaredMembers);
                    await swr.WriteAsync("</ComplexType>");
                }

                #endregion

                #region Print entity types

                foreach (var (type, members) in metadata.EntityResourceTypes.Where(t => t.Key != typeof(Metadata)))
                {
                    var (dynamicMembers, declaredMembers) = members.Split(IsDynamicMember);
                    var isOpenType = type.IsDynamic() || dynamicMembers.Any();
                    await swr.WriteAsync($"<EntityType Name=\"{type.FullName}\" OpenType=\"{isOpenType.XMLBool()}\">");
                    var key = declaredMembers.OfType<DeclaredProperty>().FirstOrDefault(p => p.HasAttribute<KeyAttribute>());
                    if (key != null) await swr.WriteAsync($"<Key><PropertyRef Name=\"{key.Name}\"/></Key>");
                    await WriteMembers(swr, declaredMembers.Where(p => !(p is DeclaredProperty {Hidden: true} d) || d.Equals(key)));
                    await swr.WriteAsync("</EntityType>");
                }
                await swr.WriteAsync("<EntityType Name=\"RESTable.DynamicResource\" OpenType=\"true\"/>");

                #endregion

                #region Write entity container and entity sets

                await swr.WriteAsync($"<EntityContainer Name=\"{EntityContainerName}\">");
                foreach (var entitySet in metadata.EntityResources.Where(t => t.Type != typeof(Metadata)))
                {
                    await swr.WriteAsync($"<EntitySet EntityType=\"{GetEdmTypeName(entitySet.Type)}\" Name=\"{entitySet.Name}\">");
                    var methods = metadata.CurrentAccessScope[entitySet].Intersect(entitySet.AvailableMethods).ToList();
                    await swr.WriteAsync(InsertableAnnotation(methods.Contains(Method.POST)));
                    await swr.WriteAsync(UpdatableAnnotation(methods.Contains(Method.PATCH)));
                    await swr.WriteAsync(DeletableAnnotation(methods.Contains(Method.DELETE)));
                    await swr.WriteAsync("</EntitySet>");
                }
                await swr.WriteAsync("</EntityContainer>");
                await swr.WriteAsync($"<Annotations Target=\"global.{EntityContainerName}\">");
                await swr.WriteAsync("<Annotation Term=\"Org.OData.Capabilities.V1.ConformanceLevel\"><EnumMember>Org.OData.Capabilities.V1." +
                                     "ConformanceLevelType/Minimal</EnumMember></Annotation>");
                await swr.WriteAsync("<Annotation Term=\"Org.OData.Capabilities.V1.SupportedFormats\">");
                await swr.WriteAsync("<Collection>");
                await swr.WriteAsync("<String>application/json;odata.metadata=minimal;IEEE754Compatible=false;odata.streaming=true</String>");
                await swr.WriteAsync("</Collection>");
                await swr.WriteAsync("</Annotation>");
                await swr.WriteAsync("<Annotation Bool=\"true\" Term=\"Org.OData.Capabilities.V1.AsynchronousRequestsSupported\"/>");
                await swr.WriteAsync("<Annotation Term=\"Org.OData.Capabilities.V1.FilterFunctions\"><Collection></Collection></Annotation>");
                await swr.WriteAsync("</Annotations>");
                await swr.WriteAsync("</Schema>");
                await swr.WriteAsync("</edmx:DataServices></edmx:Edmx>");

                #endregion
            }

            return new BinaryResult(WriteStream, ContentType);
        }

        private static async Task WriteMembers(TextWriter swr, IEnumerable<Member> members)
        {
            foreach (var member in members)
            {
                await swr.WriteAsync($"<Property Name=\"{member.Name}\" Nullable=\"{member.IsNullable.XMLBool()}\" " +
                                     $"Type=\"{GetEdmTypeName(member.Type)}\" ");
                if (member.IsReadOnly)
                    await swr.WriteAsync($">{ReadOnlyAnnotation}</Property>");
                else if (member.IsWriteOnly)
                    await swr.WriteAsync($">{WriteOnlyAnnotation}</Property>");
                else await swr.WriteAsync("/>");
            }
        }

        /// <summary>
        /// Gets an Edm type name from a .NET type
        /// </summary>
        private static string GetEdmTypeName(Type type)
        {
            if (type.IsEnum) return "global." + type.FullName;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    switch (type)
                    {
                        case var _ when type == typeof(Guid): return "Edm.Guid";
                        case var _ when type.IsNullable(out var t): return GetEdmTypeName(t);
                        case var _ when type.ImplementsGenericInterface(typeof(IDictionary<,>), out var p) && p[0] == typeof(string):
                            return "global.RESTable.DynamicResource";
                        case var _ when typeof(JValue).IsAssignableFrom(type): return "Edm.PrimitiveType";
                        case var _ when typeof(JToken).IsAssignableFrom(type): return "Edm.ComplexType";
                        case var _ when type.ImplementsGenericInterface(typeof(IEnumerable<>), out var p): return $"Collection({GetEdmTypeName(p[0])})";
                        default: return $"global.{type.FullName}";
                    }
                case TypeCode.Boolean: return "Edm.Boolean";
                case TypeCode.Byte: return "Edm.Byte";
                case TypeCode.DateTime: return "Edm.DateTimeOffset";
                case TypeCode.Decimal: return "Edm.Decimal";
                case TypeCode.Double: return "Edm.Double";
                case TypeCode.Single: return "Edm.Single";
                case TypeCode.Int16: return "Edm.Int16";
                case TypeCode.UInt16:
                case TypeCode.Int32: return "Edm.Int32";
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int64: return "Edm.Int64";
                case TypeCode.SByte: return "Edm.SByte";
                case TypeCode.Char:
                case TypeCode.String: return "Edm.String";
                default: return $"global.{type.FullName}";
            }
        }

        /// <summary>
        /// We have to know whether the member is of dynamic type. If it is, the type has to be 
        /// declared as open.
        /// </summary>
        private static bool IsDynamicMember(Member member) => member.Type == typeof(object) || typeof(JToken).IsAssignableFrom(member.Type);
    }
}