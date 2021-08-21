namespace RESTable.Json.Tests
{
    public class Holder<T>
    {
        public T Item { get; set; }
    }

    public class Holder<T1, T2>
    {
        public T1 First { get; set; }
        public T2 Second { get; set; }
    }

    public class Holder<T1, T2, T3>
    {
        public T1 First { get; set; }
        public T2 Second { get; set; }
        public T3 Third { get; set; }
    }

    public class Holder<T1, T2, T3, T4>
    {
        public T1 First { get; set; }
        public T2 Second { get; set; }
        public T3 Third { get; set; }
        public T4 Fourth { get; set; }
    }
}