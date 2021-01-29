using System;

namespace RESTable.Resources
{
    /// <inheritdoc />
    /// <summary>
    /// Thrown when RESTable encounters an invalid resource wrapper declaration
    /// </summary>
    public class InvalidResourceWrapperException : RESTableException
    {
        internal InvalidResourceWrapperException((Type wrapper, Type wrapped) types, string message) : base(ErrorCodes.ResourceWrapperError,
            $"Invalid resource wrapper declaration '{types.wrapper.GetRESTableTypeName()}' for wrapped type '{types.wrapped.GetRESTableTypeName()}'. Resource wrappers {message}") { }
    }
}