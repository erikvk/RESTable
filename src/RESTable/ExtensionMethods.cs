using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Microsoft.Extensions.DependencyInjection;
using RESTable.ContentTypeProviders;
using RESTable.DefaultProtocol;
using RESTable.Internal;
using RESTable.Meta;
using RESTable.Meta.Internal;
using RESTable.Requests;
using RESTable.Requests.Filters;
using RESTable.Requests.Processors;
using RESTable.Resources;
using RESTable.Resources.Operations;
using RESTable.Results;
using static System.StringComparison;
using static RESTable.ErrorCodes;
using static RESTable.Requests.Operators;
using UriComponents = RESTable.Requests.UriComponents;

namespace RESTable;

/// <summary>
///     Extension methods used by RESTable
/// </summary>
public static class ExtensionMethods
{
    #region Member reflection

    public static string RESTableMemberName(this MemberInfo m, bool flagged = false)
    {
        string name;
        if (m.HasAttribute(out RESTableMemberAttribute? attribute) && attribute!.Name is not null)
            name = attribute.Name;
        else name = m.Name;
        return flagged ? $"${name}" : name;
    }

    public static string RESTableParameterName(this ParameterInfo p, bool flagged = false)
    {
        return (flagged ? $"${p.Name}" : p.Name)!;
    }

    public static bool RESTableIgnored(this MemberInfo m)
    {
        return m.GetCustomAttribute<RESTableMemberAttribute>()?.Ignored == true ||
               m.HasAttribute<IgnoreDataMemberAttribute>();
    }

    public static ParameterInfo? GetCustomConstructorParameterInfo(this DeclaredProperty declaredProperty)
    {
        if (declaredProperty.Owner?.GetCustomConstructor() is not { } constructor)
            return default;
        var parameters = constructor.GetParameters();
        for (var i = 0; i < parameters.Length; i += 1)
        {
            var parameter = parameters[i];
            var parameterName = parameter.RESTableParameterName(declaredProperty.Owner.IsDictionary(out _, out _));
            if (parameterName.EqualsNoCase(declaredProperty.Name)) return parameter;
        }
        return default;
    }

    #endregion

    #region Type reflection

    public static ConstructorInfo? GetCustomConstructor(this Type type)
    {
        return type
            .GetConstructors()
            .FirstOrDefault(c => c.HasAttribute<RESTableConstructorAttribute>() || c.HasAttribute<JsonConstructorAttribute>());
    }

    public static string GetRESTableTypeName(this Type type)
    {
        return type.FullName?.Replace('+', '.') ?? throw new Exception("Could not establish the name of a type");
    }

    /// <summary>
    ///     Does this type implement the IDictionary{string, object} interface?
    /// </summary>
    public static bool IsDictionary(this Type type, out bool isWritable, out Type[]? parameters)
    {
        if (type.ImplementsGenericInterface(typeof(IDictionary<,>), out parameters))
        {
            isWritable = true;
            return true;
        }
        if (type.ImplementsGenericInterface(typeof(IReadOnlyDictionary<,>), out parameters))
        {
            isWritable = false;
            return true;
        }
        isWritable = false;
        return false;
    }

    /// <summary>
    ///     Does this type implement the IDictionary{string, object} interface?
    /// </summary>
    public static bool IsDictionary(this Type type, out bool isWritable, out Type? keyType, out Type? valueType)
    {
        if (type.ImplementsGenericInterface(typeof(IDictionary<,>), out var parameters))
        {
            isWritable = true;
            keyType = parameters![0];
            valueType = parameters[1];
            return true;
        }
        if (type.ImplementsGenericInterface(typeof(IReadOnlyDictionary<,>), out parameters))
        {
            isWritable = false;
            keyType = parameters![0];
            valueType = parameters[1];
            return true;
        }
        keyType = null;
        valueType = null;
        isWritable = false;
        return false;
    }

    /// <summary>
    ///     Evaluates to true if the given type implements either IEnumerable, IEnumerable{T} or
    ///     IAsyncEnumerable{T}, and returns the generic parameter {T} in the out parameter.
    /// </summary>
    public static bool ImplementsEnumerableInterface(this Type type, out Type? parameter)
    {
        if (type.ImplementsGenericInterface(typeof(IEnumerable<>), out var parameters) ||
            type.ImplementsGenericInterface(typeof(IAsyncEnumerable<>), out parameters))
        {
            parameter = parameters![0];
            return true;
        }
        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            parameter = typeof(object);
            return true;
        }
        parameter = null;
        return false;
    }

    internal static IList<Type> GetConcreteSubclasses(this Type baseType)
    {
        return baseType.GetSubclasses()
            .Where(type => !type.IsAbstract)
            .ToList();
    }

    internal static IEnumerable<Type> GetSubclasses(this Type baseType)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(type => type.IsSubclassOf(baseType));
    }

    internal static IEnumerable<Type> GetSubtypes(this Type baseType)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(baseType.IsAssignableFrom);
    }

    internal static bool HasAttribute(this MemberInfo type, Type attributeType)
    {
        return type.GetCustomAttributes(attributeType).Any();
    }

    public static bool HasResourceProviderAttribute(this Type resource)
    {
        return resource.GetCustomAttributes().OfType<EntityResourceProviderAttribute>().Any();
    }

    public static bool HasAttribute<TAttribute>(this MemberInfo type) where TAttribute : Attribute
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        return type.GetCustomAttribute<TAttribute>() is not null;
    }

    public static bool HasAttribute<TAttribute>(this MemberInfo type, out TAttribute? attribute) where TAttribute : Attribute
    {
        attribute = type.GetCustomAttribute<TAttribute>();
        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        return attribute is not null;
    }

    internal static IEnumerable<Type> GetBaseTypes(this Type type)
    {
        var currentBaseType = type.BaseType;
        while (currentBaseType is not null)
        {
            yield return currentBaseType;
            currentBaseType = currentBaseType.BaseType;
        }
    }

    /// <summary>
    ///     Returns true if and only if the type implements the generic interface type
    /// </summary>
    public static bool ImplementsGenericInterface(this Type type, Type interfaceType)
    {
        if (type.Name == interfaceType.Name &&
            type.Namespace == interfaceType.Namespace &&
            type.Assembly == interfaceType.Assembly)
            return true;
        return type
            .GetInterfaces()
            .Any(i => i.Name == interfaceType.Name &&
                      i.Namespace == interfaceType.Namespace &&
                      i.Assembly == interfaceType.Assembly);
    }

    /// <summary>
    ///     Returns true if and only if the type implements the generic interface type. Returns the
    ///     generic type parameters (if any) in an out parameter.
    /// </summary>
    public static bool ImplementsGenericInterface(this Type type, Type interfaceType, out Type[]? genericParameters)
    {
        var match = type.GetInterfaces()
            .FirstOrDefault(i => i.Name == interfaceType.Name &&
                                 i.Namespace == interfaceType.Namespace &&
                                 i.Assembly == interfaceType.Assembly);
        if (match is null &&
            type.Name == interfaceType.Name &&
            type.Namespace == interfaceType.Namespace &&
            type.Assembly == interfaceType.Assembly)
            match = type;
        genericParameters = match?.GetGenericArguments();
        return match is not null;
    }

    internal static long CountBytes(this Type type)
    {
        if (type.IsEnum) return 8;
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Object when type.IsNullable(out var baseType) => CountBytes(baseType!),
            TypeCode.Object => throw new Exception($"Unknown type encountered: '{type.GetRESTableTypeName()}'"),
            TypeCode.Boolean => 4,
            TypeCode.Char => 2,
            TypeCode.SByte => 1,
            TypeCode.Byte => 1,
            TypeCode.Int16 => 2,
            TypeCode.UInt16 => 2,
            TypeCode.Int32 => 4,
            TypeCode.UInt32 => 4,
            TypeCode.Int64 => 8,
            TypeCode.UInt64 => 8,
            TypeCode.Single => 4,
            TypeCode.Double => 8,
            TypeCode.Decimal => 16,
            TypeCode.DateTime => 8,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion

    #region Other

    internal static string UriEncode(this string str)
    {
        return Uri.EscapeDataString(str);
    }

    internal static string UriDecode(this string str)
    {
        return Uri.UnescapeDataString(str);
    }

    internal static bool EqualsNoCase(this string? s1, string? s2)
    {
        return string.Equals(s1, s2, OrdinalIgnoreCase);
    }

    internal static string ToMethodsString(this IEnumerable<Method> ie)
    {
        return string.Join(", ", ie);
    }

    public static string Fnuttify(this string sqlKey)
    {
        return $"\"{sqlKey.Replace(".", "\".\"")}\"";
    }

    /// <summary>
    ///     Checks if the type is a Nullable struct, and - if so -  returns the underlying type in the out parameter.
    /// </summary>
    public static bool IsNullable(this Type type, out Type? baseType)
    {
        baseType = Nullable.GetUnderlyingType(type);
        return baseType is not null;
    }

    /// <summary>
    ///     Splits a string into two parts by a separator char, and returns a 2-tuple
    ///     holding the parts.
    /// </summary>
    public static (string, string?) TupleSplit(this string str, char separator, bool trim = false)
    {
        var split = str.Split([separator], 2, StringSplitOptions.RemoveEmptyEntries);
        return trim switch
        {
            false => split.Length switch
            {
                1 => (split[0], null),
                2 => (split[0], split[1]),
                _ => throw new ArgumentOutOfRangeException()
            },
            true => split.Length switch
            {
                1 => (split[0].Trim(), null),
                2 => (split[0].Trim(), split[1].Trim()),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }

    /// <summary>
    ///     Splits a string into two parts by a separator string, and returns a 2-tuple
    ///     holding the parts.
    /// </summary>
    public static (string, string?) TupleSplit(this string str, string separator, bool trim = false)
    {
        var split = str.Split([separator], 2, StringSplitOptions.None);
        return trim switch
        {
            false => split.Length switch
            {
                1 => (split[0], null),
                2 => (split[0], split[1]),
                _ => throw new ArgumentOutOfRangeException()
            },
            true => split.Length switch
            {
                1 => (split[0].Trim(), null),
                2 => (split[0].Trim(), split[1].Trim()),
                _ => throw new ArgumentOutOfRangeException()
            }
        };
    }

    #endregion

    #region Resource helpers

    internal static bool IsDynamic(this Type type)
    {
        return typeof(IDynamicMemberValueProvider).IsAssignableFrom(type) || type.IsDictionary(out _, out _);
    }

    internal static ResourceKind GetResourceKind(this Type metatype)
    {
        return metatype switch
        {
            _ when typeof(IEntityResource).IsAssignableFrom(metatype) => ResourceKind.EntityResource,
            _ when typeof(ITerminalResource).IsAssignableFrom(metatype) => ResourceKind.TerminalResource,
            _ when typeof(IBinaryResource).IsAssignableFrom(metatype) => ResourceKind.BinaryResource,
            _ when typeof(IEventResource).IsAssignableFrom(metatype) => ResourceKind.EventResource,
            _ => ResourceKind.All
        };
    }

    internal static TermBindingRule GetBindingRule(this Type type, bool input)
    {
        return input ? type.GetInputBindingRule() : type.GetOutputBindingRule();
    }

    internal static TermBindingRule GetInputBindingRule(this Type type)
    {
        var attribute = type.GetCustomAttribute<RESTableAttribute>();
        return type.IsDynamic() || attribute?.AllowDynamicConditions == true
            ? TermBindingRule.DeclaredWithDynamicFallback
            : TermBindingRule.OnlyDeclared;
    }

    internal static TermBindingRule GetOutputBindingRule(this Type type)
    {
        return type.IsDynamic()
            ? TermBindingRule.DeclaredWithDynamicFallback
            : TermBindingRule.OnlyDeclared;
    }

    internal static (bool allowDynamic, TermBindingRule bindingRule) GetDynamicConditionHandling(this Type type, RESTableAttribute? attribute)
    {
        var dynamicConditionsAllowed = typeof(IDynamicMemberValueProvider).IsAssignableFrom(type) ||
                                       type.IsDictionary(out _, out _) ||
                                       attribute?.AllowDynamicConditions == true;
        var conditionBindingRule = dynamicConditionsAllowed ? TermBindingRule.DeclaredWithDynamicFallback : TermBindingRule.OnlyDeclared;
        return (dynamicConditionsAllowed, conditionBindingRule);
    }

    internal static Type? GetRESTableInterfaceType(this Type resourceType)
    {
        return resourceType
            .GetInterfaces()
            .FirstOrDefault(i => typeof(IEntityDefinition).IsAssignableFrom(i));
    }

    internal static IEnumerable<Operator> ToOperators(this Operators operators)
    {
        var opList = new List<Operator>();
        if (operators.HasFlag(EQUALS)) opList.Add(EQUALS);
        if (operators.HasFlag(NOT_EQUALS)) opList.Add(NOT_EQUALS);
        if (operators.HasFlag(LESS_THAN)) opList.Add(LESS_THAN);
        if (operators.HasFlag(GREATER_THAN)) opList.Add(GREATER_THAN);
        if (operators.HasFlag(LESS_THAN_OR_EQUALS)) opList.Add(LESS_THAN_OR_EQUALS);
        if (operators.HasFlag(GREATER_THAN_OR_EQUALS)) opList.Add(GREATER_THAN_OR_EQUALS);
        return opList;
    }

    /// <summary>
    ///     Converts a Dictionary object to a JsonProperty
    /// </summary>
    public static JsonProperty? GetProperty(this JsonElement obj, string name, StringComparison stringComparison = OrdinalIgnoreCase)
    {
        foreach (var property in obj.EnumerateObject())
            if (string.Equals(property.Name, name, stringComparison))
                return property;
        return default;
    }

    /// <summary>
    ///     Converts a Dictionary object to a JSON.net JObject
    /// </summary>
    public static bool GetProperty(this JsonElement obj, string name, out JsonProperty? jsonProperty, StringComparison stringComparison = OrdinalIgnoreCase)
    {
        foreach (var property in obj.EnumerateObject())
            if (string.Equals(property.Name, name, stringComparison))
            {
                jsonProperty = property;
                return true;
            }
        jsonProperty = null;
        return false;
    }

    public static async ValueTask<ProcessedEntity> MakeProcessedEntity<T>(this T entity, ISerializationMetadata metadata) where T : notnull
    {
        if (entity is ProcessedEntity processedEntity)
            return processedEntity;

        var toPopulate = entity switch
        {
            IDictionary<string, object?> dictionary => new ProcessedEntity(dictionary),
            IEnumerable<KeyValuePair<string, object?>> keyValuePairs => new ProcessedEntity(keyValuePairs),
            IDictionary dictionary => new ProcessedEntity(dictionary),
            _ => new ProcessedEntity()
        };

        foreach (var property in metadata.PropertiesToSerialize) toPopulate[property.Name] = await property.GetValue(entity).ConfigureAwait(false);
        return toPopulate;
    }

    internal static string GetEntityResourceProviderId(this Type providerType)
    {
        var typeName = providerType.Name;
        if (typeName is null) throw new ArgumentNullException();
        if (typeName.EndsWith("entityresourceprovider", InvariantCultureIgnoreCase))
            typeName = typeName[..^22];
        else if (typeName.EndsWith("resourceprovider", InvariantCultureIgnoreCase))
            typeName = typeName[..^16];
        else if (typeName.EndsWith("provider", InvariantCultureIgnoreCase))
            typeName = typeName[..^8];
        return typeName;
    }

    public static Type? GetWrappedType(this Type wrapperType)
    {
        return wrapperType.BaseType?.GetGenericArguments()[0];
    }

    public static bool IsWrapper(this Type type)
    {
        return typeof(IResourceWrapper).IsAssignableFrom(type);
    }

    #endregion

    #region Filter and Process

    internal static IAsyncEnumerable<T> Filter<T>(this IAsyncEnumerable<T> entities, IFilter? filter) where T : notnull
    {
        return filter?.Apply(entities) ?? entities;
    }

    internal static IAsyncEnumerable<ProcessedEntity> Process<T>(this IAsyncEnumerable<T> entities, IReadOnlyList<IProcessor> processors, ISerializationMetadata metadata)
        where T : notnull
    {
        var target = processors[0].Apply(entities, metadata);
        for (var i = 1; i < processors.Count; i += 1) target = processors[i].Apply(target, metadata);
        return target;
    }

    #endregion

    #region Dictionary helpers

    /// <summary>
    ///     Gets the value of a key from an IDictionary, or null if the dictionary does not contain the key.
    /// </summary>
    // ReSharper disable once ReturnTypeCanBeNotNullable
    public static TValue? SafeGet<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        dict.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    ///     Puts the tuple into the IDictionary
    /// </summary>
    public static void TuplePut<TKey, TValue>(this IDictionary<TKey, TValue> dict, (TKey key, TValue value) pair)
    {
        if (pair.key is null) throw new ArgumentNullException(nameof(pair));
        dict[pair.key] = pair.value;
    }

    public static Dictionary<TKey, T> SafeToDictionary<T, TKey>
    (
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey> equalityComparer
    )
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, T>(equalityComparer);
        foreach (var item in source)
        {
            dictionary[keySelector(item)] = item;
        }
        return dictionary;
    }

    public static Dictionary<TKey, T> SafeToDictionary<T, TKey>
    (
        this IEnumerable<T> source,
        Func<T, TKey> keySelector
    )
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, T>();
        foreach (var item in source)
        {
            dictionary[keySelector(item)] = item;
        }
        return dictionary;
    }

    public static Dictionary<TKey, TValue> SafeToDictionary<T, TKey, TValue>
    (
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector
    )
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>();
        foreach (var item in source)
        {
            dictionary[keySelector(item)] = valueSelector(item);
        }
        return dictionary;
    }

    public static Dictionary<TKey, TValue> SafeToDictionary<T, TKey, TValue>
    (
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        Func<T, TValue> valueSelector,
        IEqualityComparer<TKey> equalityComparer
    )
        where TKey : notnull
    {
        var dictionary = new Dictionary<TKey, TValue>(equalityComparer);
        foreach (var item in source)
        {
            dictionary[keySelector(item)] = valueSelector(item);
        }
        return dictionary;
    }

    /// <summary>
    ///     Gets the value of a key from an IDictionary_2, without case sensitivity, or null if the dictionary does
    ///     not contain the key. The actual key is returned in the actualKey out parameter.
    /// </summary>
    internal static bool TryFindInDictionary<T>(this IDictionary<string, T?> dict, string key, out string? actualKey, out T? result)
    {
        var matches = dict
            .Where(pair => pair.Key.EqualsNoCase(key))
            .ToList();
        switch (matches.Count)
        {
            case 0:
                result = default;
                actualKey = null;
                return false;
            case > 1 when dict.TryGetValue(key, out result):
            {
                actualKey = key;
                return true;
            }
            default:
            {
                actualKey = matches[0].Key;
                result = matches[0].Value;
                return true;
            }
        }
    }

    /// <summary>
    ///     Gets the value of a key from an IDictionary, without case sensitivity, or null if the dictionary does
    ///     not contain the key. The actual key is returned in the actualKey out parameter.
    /// </summary>
    internal static bool TryFindInDictionary(this IDictionary dict, string key, out string? actualKey, out object? result)
    {
        var matchKeys = dict.Keys
            .Cast<string>()
            .Where(key.EqualsNoCase)
            .ToList();
        switch (matchKeys.Count)
        {
            case 0:
            {
                result = default;
                actualKey = null;
                return false;
            }
            case > 1 when dict.Contains(key):
            {
                actualKey = key;
                result = dict[key];
                return true;
            }
            default:
            {
                actualKey = matchKeys[0];
                result = dict[actualKey];
                return true;
            }
        }
    }

    internal static string Capitalize(this string input)
    {
        var array = input.ToCharArray();
        array[0] = char.ToUpper(array[0]);
        return new string(array);
    }

    #endregion

    #region Requests

    internal static string GetFriendlyTypeName(this Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "a boolean (true or false)",
            TypeCode.Char => "a single character",
            TypeCode.SByte => $"an integer ({sbyte.MinValue} to {sbyte.MaxValue})",
            TypeCode.Byte => $"a positive integer ({byte.MinValue} to {byte.MaxValue})",
            TypeCode.Int16 => $"an integer ({short.MinValue} to {short.MaxValue})",
            TypeCode.UInt16 => $"a positive integer ({ushort.MinValue} to {ushort.MaxValue})",
            TypeCode.Int32 => "an integer (32-bit)",
            TypeCode.Int64 => "an integer (64-bit)",
            TypeCode.UInt32 => "a positive integer (32-bit)",
            TypeCode.UInt64 => "a positive integer (64-bit)",
            TypeCode.Single => "a floating point number (single)",
            TypeCode.Double => "a floating point number (double)",
            TypeCode.Decimal => "a floating point number",
            TypeCode.String => "a string",
            TypeCode.DateTime => "a date time",
            _ when type.IsNullable(out var baseType) => baseType!.GetFriendlyTypeName(),
            _ => type.FullName ?? type.ToString()
        };
    }

    public static Error AsError(this Exception exception)
    {
        return exception switch
        {
            Error re => re,
            FormatException _ => new UnsupportedContent(exception),
            JsonException jre => new FailedJsonDeserialization(jre),
            RuntimeBinderException _ => new BinderPermissions(exception),
            ArgumentException _ => new BadRequest(ErrorCodes.Unknown, exception.Message, exception),
            NotImplementedException _ => new FeatureNotImplemented("RESTable encountered a call to a non-implemented method"),
            _ => new Unknown(exception)
        };
    }

    public static Error AsResultOf(this Exception exception, IRequest? request, bool cancelled = false)
    {
        var error = exception.AsError();
        if (request is null) return error;
        error.SetContext(request.Context);
        error.Request = request;
        if (!cancelled && error is not Forbidden && request.Method >= 0)
        {
            var errorId = Admin.Error.Create(error, request).Id;
            error.Headers.Error = $"/restable.admin.error/id={errorId}";
        }
        if (request.Headers.Metadata?.EqualsNoCase("full") == true)
            error.Headers.Metadata = error.Metadata;
        error.Headers.Version = request.GetRequiredService<RESTableConfiguration>().Version;
        return error;
    }

    /// <summary>
    ///     Creates a new writeable UriComponents instance from a possibly read-only IUriComponents instance
    /// </summary>
    public static UriComponents ToWritable(this IUriComponents components)
    {
        return components as UriComponents ?? new UriComponents(components);
    }

    /// <summary>
    ///     Generates new UriComponents that encode a request for the next page of entities, calculated from an IEntities
    ///     entity collection.
    ///     The count parameter controls the size of the next page. If omitted, the size is the same as the current page.
    /// </summary>
    public static IUriComponents GetNextPageLink(this IEntities entities, long entityCount, int nextPageSize)
    {
        var components = entities.Request.UriComponents.ToWritable();
        if (nextPageSize > -1)
        {
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
            components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, nextPageSize.ToString()));
        }
        components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Offset)));
        var previousOffset = entities.Request.MetaConditions.Offset;
        var offsetNr = entityCount + previousOffset.Number;
        UriCondition offset;
        if (previousOffset == -1)
            offset = new UriCondition(RESTableMetaCondition.Offset, "∞");
        else if (previousOffset < -1 && offsetNr >= -1)
            offset = new UriCondition(RESTableMetaCondition.Offset, (-1).ToString());
        else offset = new UriCondition(RESTableMetaCondition.Offset, offsetNr.ToString());
        components.MetaConditions.Add(offset);
        return components;
    }

    /// <summary>
    ///     Generates new UriComponents that encode a request for the previous page of entities, calculated from an IEntities
    ///     entity collection.
    ///     The count parameter controls the size of the next page. If omitted, the size is the same as the current page.
    /// </summary>
    public static IUriComponents GetPreviousPageLink(this IEntities entities, long entityCount, int nextPageSize = -1)
    {
        var components = entities.Request.UriComponents.ToWritable();
        var previousOffset = entities.Request.MetaConditions.Offset;
        var pageSize = entityCount;
        if (nextPageSize > -1)
        {
            components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
            components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, nextPageSize.ToString()));
            pageSize = nextPageSize;
        }
        var offsetNr = previousOffset.Number - pageSize;
        components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Offset)));
        UriCondition offset;
        if (previousOffset == 0)
        {
            offset = new UriCondition(RESTableMetaCondition.Offset, "-∞");
        }
        else if (previousOffset > 0 && offsetNr <= 0)
        {
            if (long.TryParse(components.MetaConditions.FirstOrDefault(c => c.Key.EqualsNoCase(nameof(Limit)))?.ValueLiteral, out var limit))
            {
                components.MetaConditions.RemoveAll(c => c.Key.EqualsNoCase(nameof(Limit)));
                components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, (limit + offsetNr).ToString()));
            }
            offset = new UriCondition(RESTableMetaCondition.Offset, 0.ToString());
        }
        else
        {
            offset = new UriCondition(RESTableMetaCondition.Offset, offsetNr.ToString());
        }
        components.MetaConditions.Add(offset);
        return components;
    }

    /// <summary>
    ///     Generates new UriComponents that encode a request for the first number of entities, calculated from an IEntities
    ///     entity collection.
    ///     The count parameter controls how many entities are selected. If omitted, one entity is selected.
    /// </summary>
    public static ValueTask<IUriComponents> GetFirstLink(this IEntities entities, int count = 1)
    {
        var components = entities.Request.UriComponents.ToWritable();
        components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
        components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, count.ToString()));
        return new ValueTask<IUriComponents>(components);
    }

    /// <summary>
    ///     Generates new UriComponents that encode a request for the last number of entities, calculated from an IEntities
    ///     entity collection.
    ///     The count parameter controls how many entities are selected. If omitted, one entity is selected.
    /// </summary>
    public static ValueTask<IUriComponents> GetLastLink(this IEntities entities, int count = 1)
    {
        var components = entities.Request.UriComponents.ToWritable();
        components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
        components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Offset, (-count).ToString()));
        components.MetaConditions.Add(new UriCondition(RESTableMetaCondition.Limit, count.ToString()));
        return new ValueTask<IUriComponents>(components);
    }

    /// <summary>
    ///     Generates new UriComponents that encode a request for all entities in a resource, calculated from an IEntities
    ///     entity collection.
    /// </summary>
    public static ValueTask<IUriComponents> GetAllLink(this IEntities entities)
    {
        var components = entities.Request.UriComponents.ToWritable();
        components.MetaConditions.RemoveAll(m => m.Key.EqualsNoCase(nameof(Offset)) || m.Key.EqualsNoCase(nameof(Limit)));
        return new ValueTask<IUriComponents>(components);
    }

    public static IContentTypeProvider GetInputContentTypeProvider(this IProtocolHolder protocolHolder, ContentType? contentTypeOverride = null)
    {
        var contentType = contentTypeOverride ?? protocolHolder.Headers.ContentType ?? protocolHolder.CachedProtocolProvider.DefaultInputProvider.ContentType;
        if (!protocolHolder.CachedProtocolProvider.InputMimeBindings.TryGetValue(contentType.MediaType, out var contentTypeProvider))
            throw new UnsupportedContent(contentType.ToString());
        return contentTypeProvider;
    }

    /// <summary>
    ///     Checks if the given protocol holder can accept a request with the given content type
    /// </summary>
    public static bool Accepts(this IProtocolHolder protocolHolder, ContentType contentType, out string? acceptHeader)
    {
        var accept = protocolHolder.Headers.Accept;
        if (accept is not null)
        {
            acceptHeader = accept.ToString();
            foreach (var acceptType in accept)
            {
                if (acceptType.AnyType) return true;
                if (protocolHolder.CachedProtocolProvider.OutputMimeBindings.TryGetValue(contentType.MediaType, out _))
                    return true;
            }
            acceptHeader = null;
            return false;
        }
        // No accept header means accept anything
        acceptHeader = null;
        return true;
    }

    public static IContentTypeProvider GetOutputContentTypeProvider(this IProtocolHolder protocolHolder, ContentType? contentTypeOverride = null)
    {
        IContentTypeProvider? acceptProvider = null;

        var protocolProvider = protocolHolder.CachedProtocolProvider;
        var headers = protocolHolder.Headers;
        var contentType = contentTypeOverride;
        if (contentType.HasValue)
            contentType = contentType.Value;
        else if (!(headers.Accept?.Count > 0))
            contentType = protocolProvider.DefaultOutputProvider.ContentType;
        if (!contentType.HasValue)
        {
            var containedWildcard = false;
            var foundProvider = headers.Accept!.Any(a =>
            {
                if (!a.AnyType)
                    return protocolProvider.OutputMimeBindings.TryGetValue(a.MediaType, out acceptProvider);
                containedWildcard = true;
                return false;
            });
            if (!foundProvider)
                if (containedWildcard)
                    acceptProvider = protocolProvider.DefaultOutputProvider;
                else throw new NotAcceptable(headers.Accept!.ToString());
        }
        else if (!protocolProvider.OutputMimeBindings.TryGetValue(contentType.Value.MediaType, out acceptProvider))
        {
            throw new NotAcceptable(contentType.Value.ToString());
        }
        return acceptProvider!;
    }

    /// <summary>
    ///     Is this type a type that can be encoded in a RESTable request URI value literal?
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsOfValueLiteralType(this Type type)
    {
        switch (type)
        {
            case var _ when type.IsNullable(out var baseType): return IsOfValueLiteralType(baseType!);
            case var _ when type.IsEnum:
            case var _ when type == typeof(DBNull):
            case var _ when type == typeof(bool):
            case var _ when type == typeof(decimal):
            case var _ when type == typeof(long):
            case var _ when type == typeof(sbyte):
            case var _ when type == typeof(byte):
            case var _ when type == typeof(short):
            case var _ when type == typeof(ushort):
            case var _ when type == typeof(int):
            case var _ when type == typeof(uint):
            case var _ when type == typeof(ulong):
            case var _ when type == typeof(float):
            case var _ when type == typeof(double):
            case var _ when type == typeof(string):
            case var _ when type == typeof(DateTime):
            case var _ when type == typeof(char):
                return true;
        }
        return false;
    }

    /// <summary>
    ///     Parses a condition value from a value literal, and performs an optional type check (non-optional for enums)
    /// </summary>
    internal static object? ParseConditionValue(this string valueLiteral, DeclaredProperty? property = null)
    {
        switch (valueLiteral)
        {
            case null:
            case "null": return null;
            case "": return "";
        }

        if (property is { IsEnum: true } prop)
            try
            {
                if (prop.IsNullable)
                {
                    var type = Nullable.GetUnderlyingType(prop.Type) ?? prop.Type;
                    return Enum.Parse(type, valueLiteral, true);
                }
                return Enum.Parse(property.Type, valueLiteral, true);
            }
            catch
            {
                throw new InvalidSyntax(InvalidEnumValue, $"'{valueLiteral}' is not a valid enum value for property '{property.Name}'");
            }
        var (first, length) = (valueLiteral[0], valueLiteral.Length);
        var escapedString = false;
        switch (first)
        {
            case '\'':
            case '\"':
                if (length > 1 && valueLiteral[length - 1] == first)
                {
                    valueLiteral = valueLiteral.Substring(1, length - 2);
                    escapedString = true;
                }
                break;
        }
        if (property is not null)
        {
            var targetType = property.Type.IsNullable(out var t) ? t! : property.Type;
            try
            {
                if (property.Type == typeof(DateTimeOffset))
                {
                    if (DateTimeOffset.TryParseExact(valueLiteral, PartialIsoFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTimeOffset))
                        return dateTimeOffset.ToUniversalTime();
                }
                if (property.IsDateTime)
                {
                    if (DateTime.TryParseExact(valueLiteral, PartialIsoFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                        return dateTime.ToUniversalTime();
                    throw new Exception();
                }
                return Convert.ChangeType(valueLiteral, targetType);
            }
            catch (InvalidCastException) when (targetType.IsClass)
            {
                // In this case, we could not cast the value literal to some class type. 
                // We should just keep it as literal for now, and see if the resource wants
                // to use it as string.
                return valueLiteral;
            }
            catch
            {
                throw new InvalidConditionValueType(valueLiteral, property);
            }
        }
        if (escapedString) return valueLiteral;
        switch (valueLiteral)
        {
            case "false":
            case "False":
            case "FALSE": return false;
            case "true":
            case "True":
            case "TRUE": return true;
        }
        if (char.IsDigit(first))
        {
            if (int.TryParse(valueLiteral, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
                return i;
            if (decimal.TryParse(valueLiteral, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return d;
            if (DateTime.TryParseExact(valueLiteral, PartialIsoFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateTime))
                return dateTime.ToUniversalTime();
        }
        return valueLiteral;
    }

    private static readonly string[] PartialIsoFormats =
    [
        "yyyy-MM-ddTHH:mm:ss.fffffffK", // Complete with fractions of a second
        "yyyy-MM-ddTHH:mm:ssK", // Complete without fractions of a second
        "yyyy-MM-ddTHH:mmK", // Up to minutes
        "yyyy-MM-dd" // Only date
    ];

    #endregion

    #region Conversion

    internal static (int offset, int limit) ToOffsetAndLimit(this Range range)
    {
        var offset = range.Start switch
        {
            { IsFromEnd: true, Value: 0 } when range.End.Equals(^0) => int.MaxValue,
            { IsFromEnd: true, Value: 0 } => throw new ArgumentOutOfRangeException(nameof(range)),
            { IsFromEnd: true } => -range.Start.Value,
            _ => range.Start.Value
        };
        return range.End switch
        {
            { IsFromEnd: true } when range.End.Value != 0 => throw new ArgumentOutOfRangeException
            (
                nameof(range), "Ranges where End.FromEnd == true and End.Value > 0 are not supported by RESTable"
            ),
            { IsFromEnd: true } => (offset, -1),
            _ => range.Start switch
            {
                { IsFromEnd: true } => throw new ArgumentOutOfRangeException
                (
                    nameof(range), "Ranges where Start.FromEnd == true and End.FromEnd == false are not supported by RESTable"
                ),
                _ when range.End.Value - range.Start.Value is var limit => limit < 0
                    ? throw new ArgumentOutOfRangeException(nameof(range), "Negative ranges are not supported by RESTable")
                    : (offset, limit),
                _ => throw new Exception($"Unknown error while parsing start of range '{range}'")
            }
        };
    }

    internal static (int offset, int limit) ToSlicedOffsetAndLimit(this Range range, int currentOffset, int currentLimit)
    {
        switch (currentLimit)
        {
            case < 0:
            {
                var (rangeOffset, rangeLimit) = range.ToOffsetAndLimit();
                switch (currentOffset)
                {
                    case < 0: return (currentLimit + rangeOffset, rangeLimit);
                    case 0:
                    case > 0 when rangeOffset < 0: return (rangeOffset, rangeLimit);
                    case > 0: return (rangeOffset + currentOffset, rangeLimit);
                }
            }
            case 0: return (currentOffset, currentLimit);
            case > 0:
            {
                var (offset, length) = range.GetOffsetAndLength(currentLimit);
                return (offset + currentOffset, length);
            }
        }
    }

    internal static double GetRESTableElapsedMs(this TimeSpan timeSpan)
    {
        return Math.Round(timeSpan.TotalMilliseconds, 4);
    }

    internal static string ToStringRESTable(this TimeSpan timeSpan)
    {
        return timeSpan.GetRESTableElapsedMs().ToString(CultureInfo.InvariantCulture);
    }

    internal static byte[] ToBytes(this string str)
    {
        return Encoding.UTF8.GetBytes(str);
    }

    internal static byte[] ToByteArray(this Stream stream)
    {
        switch (stream)
        {
            case MemoryStream ms: return ms.ToArray();
            case Body body: return body.GetBytes();
            default:
            {
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);
                return ms.ToArray();
            }
        }
    }

    internal static async Task<byte[]> ToByteArrayAsync(this Stream stream)
    {
        switch (stream)
        {
            case MemoryStream ms: return ms.ToArray();
            default:
            {
                var ms = new MemoryStream();
                await using (ms.ConfigureAwait(false))
                {
                    await stream.CopyToAsync(ms).ConfigureAwait(false);
                    if (stream.CanSeek)
                        stream.Seek(0, SeekOrigin.Begin);
                    return ms.ToArray();
                }
            }
        }
    }

    /// <summary>
    ///     Converts a boolean into an XML boolean string, i.e. "true" or "false"
    /// </summary>
    /// <param name="bool"></param>
    /// <returns></returns>
    public static string XMLBool(this bool @bool)
    {
        const string trueString = "true";
        const string falseString = "false";
        if (@bool)
            return trueString;
        return falseString;
    }

    /// <summary>
    ///     Converts an HTTP status code to the underlying numeric code
    /// </summary>
    internal static ushort? ToCode(this HttpStatusCode statusCode)
    {
        return (ushort) statusCode;
    }

    /// <summary>
    ///     Creates a formatted string representation of the URI components,
    ///     a valid URI string according to the assigned protocol.
    /// </summary>
    public static string ToUriString(this IUriComponents uriComponents)
    {
        var protocolProvider = uriComponents.ProtocolProvider;
        var uriString = protocolProvider.MakeRelativeUri(uriComponents);
        if (protocolProvider is DefaultProtocolProvider)
            return uriString;
        return $"-{protocolProvider.ProtocolIdentifier}{uriString}";
    }

    internal static string ReplaceFirst(this string text, string search, string replace, out bool replaced)
    {
        var pos = text.IndexOf(search, Ordinal);
        if (pos < 0)
        {
            replaced = false;
            return text;
        }

        replaced = true;
        return $"{text[..pos]}{replace}{text[(pos + search.Length)..]}";
    }

    internal static List<InvalidMember> ToInvalidMembers(this IEnumerable<ParameterInfo> missingParameters, Type owner)
    {
        return missingParameters
            .Select(parameter => new InvalidMember
            (
                owner,
                parameter.RESTableParameterName(owner.IsDictionary(out _, out _)),
                parameter.ParameterType,
                $"Missing parameter of type '{parameter.ParameterType}'"
            )).ToList();
    }

    #endregion
}
