namespace RESTar.Admin
{
    /// <summary>
    /// The types of views in RESTar
    /// </summary>
    public enum ViewType
    {
        /// <summary>
        /// Static views are created at compile time
        /// </summary>
        Static,

        /// <summary>
        /// Dynamic views are created at runtime
        /// </summary>
        Dynamic
    }

    //    [Database, RESTar]
    //    public class DynamicView
    //    {
    //        public string Name { get; set; }
    //        public string Description { get; set; }
    //        public ViewType Type => ViewType.Dynamic;
    //        public string Resource { get; set; }
    //        public string SourceCode { get; set; }
    //    }
}