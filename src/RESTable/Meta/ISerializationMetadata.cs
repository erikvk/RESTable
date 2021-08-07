namespace RESTable.Meta
{
    public interface ISerializationMetadata
    {
        bool HasParameterLessConstructor { get; }
        object? CreateInstance();
        DeclaredProperty[] PropertiesToSerialize { get; }
        DeclaredProperty? GetProperty(string name);
        bool TypeIsDynamic { get; }
    }

    public interface ISerializationMetadata<T> : ISerializationMetadata
    {
        new T CreateInstance();
    }
}