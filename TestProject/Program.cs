using System.Threading.Tasks;
using Starcounter;

// ReSharper disable All

namespace TestProject
{
    public class Program
    {
        public static void Main()
        {
            Task<int> run() => Task.Run(() => "asd".Length);

            var s = run().Result;

        }
    }

    [Database]
    public class TestClass
    {
        public string NonTransient { get; set; }
        public string Transient { get; set; }
    }
}