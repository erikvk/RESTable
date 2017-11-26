namespace RESTar
{
    internal struct Formatter
    {
        internal string Pre { get; }
        internal string Post { get; }
        internal int StartIndent { get; }

        public Formatter(string pre, string post, int startIndent)
        {
            Pre = pre;
            Post = post;
            StartIndent = startIndent;
        }
    }
}