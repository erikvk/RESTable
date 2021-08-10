namespace RESTable.Meta
{
    public interface ISerializationMetadata
    {
        bool HasParameterLessConstructor { get; }
        object? CreateInstance();
        DeclaredProperty[] PropertiesToSerialize { get; }
        DeclaredProperty? GetProperty(string name);
        bool TypeIsDictionary { get; }
        bool TypeIsWritableDictionary { get; }
    }

    public interface ISerializationMetadata<T> : ISerializationMetadata
    {
        new T CreateInstance();
    }
}