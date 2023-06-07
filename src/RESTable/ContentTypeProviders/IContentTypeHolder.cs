namespace RESTable.ContentTypeProviders;

public interface IContentTypeHolder
{
    IContentTypeProvider InputContentTypeProvider { get; }
    IContentTypeProvider OutputContentTypeProvider { get; }
}
