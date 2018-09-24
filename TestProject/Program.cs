using System;
using System.Collections.Generic;

namespace TestProject
{
    public class Program
    {
        public static void Main() { }

        private static IEnumerable<(int, TimeSpan)> GetThings()
        {
            yield return (default, default);
        }
    }
}