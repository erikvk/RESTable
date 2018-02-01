using Simple.OData.Client;

// ReSharper disable All

namespace ODataTestCient
{
    class Program
    {
        static void Main()
        {
            var client = new ODataClient("http://localhost:8282/rest-odata");
        }
    }
}