using Starcounter;

namespace RESTar
{
    internal delegate Response Evaluator(Requests.Request request);
    public delegate dynamic Getter(object target);
    public delegate void Setter(object target, dynamic value);
}