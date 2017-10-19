namespace RESTar
{
    /// <summary>
    /// RESTar supports windows and linux line endings when writing JSON
    /// </summary>
    public enum LineEndings
    {
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