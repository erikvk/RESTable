using System.Net.Http;

namespace RESTable.Client
{
    internal static class HttpClientManager
    {
        internal static readonly HttpClient HttpClient;
        static HttpClientManager() => HttpClient = new HttpClient();
    }
}