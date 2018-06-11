namespace RESTar.Requests
{
    internal struct Formatter
    {
        internal string Name { get; }
        internal string Pre { get; }
        internal string Post { get; }
        internal int StartIndent { get; }

        public Formatter(string name, string pre, string post, int startIndent)
        {
            Name = name;
            Pre = pre;
            Post = post;
            StartIndent = startIndent;
        }
    }
}