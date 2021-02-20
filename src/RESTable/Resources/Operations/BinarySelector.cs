using RESTable.Requests;

namespace RESTable.Resources.Operations
{
    /// <summary>
    /// Selects a stream and content type for a binary resource
    /// </summary>
    internal delegate BinaryResult BinarySelector<T>(IRequest<T> request) where T : class;
}