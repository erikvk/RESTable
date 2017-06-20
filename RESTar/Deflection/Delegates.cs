namespace RESTar.Deflection
{
    public delegate dynamic Getter(object target);

    public delegate void Setter(object target, dynamic value);

    delegate TResult RefGetter<TArg, out TResult>(ref TArg arg);
}