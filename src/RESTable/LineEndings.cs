namespace RESTable
{
    /// <summary>
    /// RESTable supports windows and linux line endings when writing JSON. To change
    /// the line endings format, include a <see cref="LineEndings"/> instance in the 'lineEndings'
    /// parameter in the call to <see cref="RESTableConfigurator.Init"/>.
    /// </summary>
    public enum LineEndings
    {
        /// <summary>
        /// Line endings are written as defined by the current environment
        /// </summary>
        Environment = 0,

        /// <summary>
        /// Line endings are written as \r\n
        /// </summary>
        Windows,

        /// <summary>
        /// Line endings are written as \n
        /// </summary>
        Linux
    }
}