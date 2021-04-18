using RESTable.Meta;

namespace RESTable.Results
{
    /// <inheritdoc />
    internal class MissingConstructorParameter : BadRequest
    {
        /// <inheritdoc />
        public MissingConstructorParameter(ITerminalResource terminal, string[] missingParameters) : base(ErrorCodes.MissingConstructorParameter,
            $"Missing parameter(s): {string.Join(", ", missingParameters)} in constructor call to {terminal.Name}") { }
    }
}