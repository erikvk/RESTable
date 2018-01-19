namespace RESTar
{
    /// <summary>
    /// Contains some information about a resource view
    /// </summary>
    internal class ViewInfo
    {
        public string Name { get; }
        public string Description { get; }
        public ViewInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}