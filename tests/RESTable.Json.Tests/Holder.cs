using RESTable.Resources;

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

    public class ParameterizedHolder<T>
    {
        public T Item { get; }

        [RESTableConstructor]
        public ParameterizedHolder(T item)
        {
            Item = item;
        }
    }

    public class ParameterizedHolder<T1, T2>
    {
        public T1 First { get; }
        public T2 Second { get; }

        [RESTableConstructor]
        public ParameterizedHolder(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }

    public class ParameterizedHolder<T1, T2, T3>
    {
        public T1 First { get; }
        public T2 Second { get; }
        public T3 Third { get; }

        [RESTableConstructor]
        public ParameterizedHolder(T1 first, T2 second, T3 third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }

    public class ParameterizedHolder<T1, T2, T3, T4>
    {
        public T1 First { get; }
        public T2 Second { get; }
        public T3 Third { get; }
        public T4 Fourth { get; }

        [RESTableConstructor]
        public ParameterizedHolder(T1 first, T2 second, T3 third, T4 fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }
    }
}