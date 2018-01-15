using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using RESTar.Admin;
using RESTar.Deflection;
using RESTar.Results.Success;
using Starcounter;

namespace RESTar.OData
{
    internal class MetadataDocument : OK
    {
        private const string ReadOnlyAnnotation = "<Annotation Term=\"Org.OData.Core.V1.Permissions\">" +
                                                  "<EnumMember>Org.OData.Core.V1.Permission/Read</EnumMember>" +
                                                  "</Annotation>";

        private static string InsertableAnnotation(bool state) => "<Annotation Term=\"Org.OData.Capabilities.V1.InsertRestrictions\">" +
                                                                  $"<Record><PropertyValue Bool=\"{state}\" Property=\"Insertable\"/>" +
                                                                  "<PropertyValue Property=\"NonInsertableNavigationProperties\">" +
                                                                  "<Collection/></PropertyValue></Record></Annotation>";

        private static string UpdatableAnnotation(bool state) => "<Annotation Term=\"Org.OData.Capabilities.V1.UpdateRestrictions\">" +
                                                                 $"<Record><PropertyValue Bool=\"{state}\" Property=\"Updatable\"/>" +
                                                                 "<PropertyValue Property=\"NonUpdatableNavigationProperties\">" +
                                                                 "<Collection/></PropertyValue></Record></Annotation>";

        private static string DeletableAnnotation(bool state) => "<Annotation Term=\"Org.OData.Capabilities.V1.DeleteRestrictions\">" +
                                                                 $"<Record><PropertyValue Bool=\"{state}\" Property=\"Deletable\"/>" +
                                                                 "<PropertyValue Property=\"NonDeletableNavigationProperties\">" +
                                                                 "<Collection/></PropertyValue></Record></Annotation>";

        private const string EntityContainerName = "DefaultContainer";

        internal MetadataDocument(Metadata metadata)
        {
            ContentType = "application/xml";
            Body = new MemoryStream();
            using (var swr = new StreamWriter(Body, Serialization.Serializer.UTF8, 1024, true))
            {
                swr.WriteXMLHeader();
                swr.Write("<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\"><edmx:DataServices>");
                swr.Write("<Schema Namespace=\"global\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">");
                foreach (var enumType in metadata.EnumTypes)
                {
                    swr.Write($"<EnumType Name=\"{enumType.FullName}\">");
                    foreach (var member in EnumMember.GetMembers(enumType))
                        swr.Write($"<Member Name=\"{member.Name}\" Value=\"{member.Value}\"/>");
                    swr.Write("</EnumType>");
                }
//                foreach (var entityType in metadata.EntityTypes)
//                {
//                    swr.Write($"<EntityType Name=\"{entityType.FullName}\" OpenType=\"{entityType.IsDynamic()}\">");
//                    foreach (var property in entityType.GetDeclaredProperties().Values.Where(p => p.Readable && !p.Hidden))
//                    {
//                        swr.Write($"<Property Name=\"{property.Name}\" Nullable=\"{property.Nullable}\" " +
//                                  $"Type=\"{property.Type.GetEdmTypeName()}\"");
//                        swr.Write(property.ReadOnly ? $">{ReadOnlyAnnotation}</Property>" : "/>");
//                    }
//                    swr.Write("</EntityType>");
//                }
                swr.Write("<EntityType Name=\"RESTar.DynamicResource\" OpenType=\"true\"/>");
                swr.Write($"<EntityContainer Name=\"{EntityContainerName}\">");
                foreach (var entitySet in metadata.EntitySets.Except(new Internal.IEntityResource[]
                {
                    Resource<AvailableResource>.Get, Resource<Admin.Console>.Get, Resource<Schema>.Get, Resource<Echo>.Get,
                    Resource<ResourceProfile>.Get, Resource<OutputFormat>.Get, Resource<Macro>.Get, Resource<AdminTools>.Get,
                    Resource<Aggregator>.Get, Resource<SetOperations>.Get
                }))
                {
                    swr.Write($"<EntitySet EntityType=\"{entitySet.Type.GetEdmTypeName()}\" Name=\"{entitySet.FullName}\">");
                    var methods = metadata.CurrentAccessRights[entitySet];
                    swr.Write(InsertableAnnotation(methods.Contains(Methods.POST)));
                    swr.Write(UpdatableAnnotation(methods.Contains(Methods.PATCH)));
                    swr.Write(DeletableAnnotation(methods.Contains(Methods.DELETE)));
                    swr.Write("</EntitySet>");
                }
                swr.Write("</EntityContainer>");
                swr.Write($"<Annotations Target=\"global.{EntityContainerName}\">");
                swr.Write("<Annotation Term=\"Org.OData.Capabilities.V1.ConformanceLevel\"><EnumMember>Org.OData.Capabilities.V1." +
                          "ConformanceLevelType/Minimal</EnumMember></Annotation>");
                swr.Write("<Annotation Term=\"Org.OData.Capabilities.V1.SupportedFormats\">");
                swr.Write("<Collection>");
                swr.Write("<String>application/json;odata.metadata=minimal;IEEE754Compatible=false;odata.streaming=true</String>");
                swr.Write("</Collection>");
                swr.Write("</Annotation>");
                swr.Write("<Annotation Bool=\"true\" Term=\"Org.OData.Capabilities.V1.AsynchronousRequestsSupported\"/>");
                swr.Write("<Annotation Term=\"Org.OData.Capabilities.V1.FilterFunctions\"><Collection></Collection></Annotation>");
                swr.Write("</Annotations>");
                swr.Write("</Schema>");
                swr.Write("</edmx:DataServices></edmx:Edmx>");
            }
            Body.Seek(0, SeekOrigin.Begin);
        }
    }

    internal static class MetadataExtensions
    {
        internal static void WriteXMLHeader(this StreamWriter swr)
        {
            swr.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        }

        internal static string GetEdmTypeName(this Type type)
        {
            if (type.IsEnum) return "global." + type.FullName;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    switch (type)
                    {
                        case var _ when type == typeof(Binary): return "Edm.Binary";
                        case var _ when type == typeof(Guid): return "Edm.Guid";
                        case var _ when type.IsNullable(out var t): return GetEdmTypeName(t);
                        case var _ when type.Implements(typeof(IDictionary<,>), out var p) && p[0] == typeof(string):
                            return "global.RESTar.DynamicResource";
                        case var _ when type == typeof(JToken) || type.IsSubclassOf(typeof(JToken)):
                        case var _ when type == typeof(object): return "Edm.ComplexType";
                        case var _ when type.Implements(typeof(IEnumerable<>), out var p): return $"Collection({GetEdmTypeName(p[0])})";
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
                default: return "global." + type.FullName;
            }
        }
    }
}