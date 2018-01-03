using System.IO;
using System.Net;
using RESTar.Requests;

namespace RESTar.Operations
{
    internal interface IFinalizedResult
    {
        HttpStatusCode StatusCode { get; }
        string StatusDescription { get; }
        Stream Body { get; }
        string ContentType { get; }
        Headers Headers { get; }
    }
}