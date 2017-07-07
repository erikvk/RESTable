using Starcounter;
using RESTar.Requests;

namespace RESTar
{
    internal delegate Response RESTEvaluator<T>(RESTRequest<T> request) where T : class;
}