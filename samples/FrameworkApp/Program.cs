using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using RESTable;
using RESTable.Requests;
using RESTable.Resources;
using RESTable.Results;

namespace FrameworkApp
{
    [RESTable, InMemory]
    public class Person
    {
        private static int Counter;

        public int Id { get; }
        public string Name { get; set; }
        public int? FavoriteNumber { get; set; }
        public Dictionary<string, object> Properties { get; }
        public int? BestFriendId { get; set; }

        public Person BestFriend => InMemoryOperations<Person>
            .Select()
            .FirstOrDefault(item => item.Id == BestFriendId);

        public Person()
        {
            Counter += 1;
            Id = Counter;
            Properties = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// This app is just to show how RESTable can run in a console app on .NET Framework. In this case 4.6.1, since
    /// it's the earliest version to support .NET Standard 2.0.
    /// </summary>
    public class Program
    {
        // METHOD URI
        public static void Main()
        {
            // Build services
            using var services = new ServiceCollection()
                .AddRESTable()
                .AddJsonProvider()
                .BuildServiceProvider();
            var configurator = services.GetRequiredService<RESTableConfigurator>();
            configurator.ConfigureRESTable();
            var rootClient = services.GetRequiredService<RootClient>();
            var context = new RESTableContext(rootClient, services);

            // Welcome
            Console.WriteLine("App is running...");
            Console.WriteLine("Enter a request like so: <METHOD> <URI> [<BODY>], e.g. POST /person {'Name': 'Jane'}");

            // Handle input loop
            while (true)
            {
                // Handle input
                var input = Console.In.ReadLine()?.TrimStart();
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                var args = input.Split(new[] {' '}, 3);
                var method = (Method) Enum.Parse(typeof(Method), args[0], ignoreCase: true);
                var uri = args[1];
                var body = args.ElementAtOrDefault(2);

                // Make request
                var request = context.CreateRequest(method, uri, body);
                using var result = request.GetResult().Result;
                using var serializedResult = result.Serialize().Result;

                // Write output
                Console.WriteLine($"=> {request.GetLogMessage().Result} {request.GetLogContent().Result}");
                Console.WriteLine($"<= {result.GetLogMessage().Result}");
                foreach (var (key, value) in result.Headers)
                    Console.WriteLine($"{key}: {value}");
                using var streamReader = new StreamReader(serializedResult.Body);
                var resultBody = streamReader.ReadToEnd();
                if (!string.IsNullOrWhiteSpace(resultBody))
                    Console.WriteLine(resultBody);
                Console.WriteLine("- - - - - - - - - - - - - - - - -");
            }
        }
    }
}