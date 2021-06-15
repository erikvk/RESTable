using System.Reflection;
using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Resources;

namespace RESTable.SQLite.Meta
{
    /// <summary>
    /// An object representing a property of a CLR class
    /// </summary>
    public class CLRProperty
    {
        private ColumnMapping? Mapping { get; set; }

        /// <summary>
        /// The name of the CLR property
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of the CLR property, defined as a TypeCode
        /// </summary>
        public CLRDataType Type { get; }

        /// <summary>
        /// Is this CLR property declared, or was it defined at runtime?
        /// </summary>
        public bool IsDeclared { get; }

        /// <summary>
        /// Is this CLR property ignored when materializing entities from SQL?
        /// </summary>
        public bool IsIgnored { get; private set; }

        /// <summary>
        /// The getter for the property value
        /// </summary>
        [RESTableMember(ignore: true)]
        public Getter? Getter { get; }

        /// <summary>
        /// The setter for the property value
        /// </summary>
        [RESTableMember(ignore: true)]
        public Setter? Set { get; }

        /// <summary>
        /// The optional SQLiteMemberAttribute associated with this CLR property
        /// </summary>
        public SQLiteMemberAttribute MemberAttribute { get; }

        internal void SetMapping(ColumnMapping mapping)
        {
            Mapping = mapping;
            IsIgnored = Type == CLRDataType.Unsupported
                        || Name == "RowId"
                        || mapping.TableMapping.TableMappingKind == TableMappingKind.Static && !IsDeclared;
        }

        /// <summary>
        /// From CLR
        /// </summary>
        /// <param name="propertyInfo"></param>
        public CLRProperty(PropertyInfo propertyInfo)
        {
            Name = propertyInfo.Name;
            Type = propertyInfo.PropertyType.ResolveCLRTypeCode();
            Getter = propertyInfo.MakeDynamicGetter();
            Set = propertyInfo.MakeDynamicSetter();
            MemberAttribute = propertyInfo.GetCustomAttribute<SQLiteMemberAttribute>();
            IsDeclared = true;
        }

        /// <summary>
        /// From SQL
        /// </summary>
        public CLRProperty(string name, CLRDataType typeCode)
        {
            MemberAttribute = null!;
            Name = name;
            Type = typeCode;
            Getter = obj =>
            {
                object? value = null;
                if (obj is IDynamicMemberValueProvider dm && dm.TryGetValue(Name, out value, out var actualKey))
                {
                    Name = actualKey;
                }
                return new ValueTask<object?>(value);
            };
            Set = (obj, value) =>
            {
                (obj as IDynamicMemberValueProvider)?.TrySetValue(Name, value);
                return default;
            };
            IsDeclared = false;
        }
    }
}