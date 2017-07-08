using Starcounter;
using RESTar.Requests;

namespace RESTar
{
    internal delegate Response RESTEvaluator<T>(RESTRequest<T> request) where T : class;

    internal delegate T2 ViewEvaluator<T1, out T2>(ViewRequest<T1> request) where T1 : class where T2 : Json, IRequest, new();
}