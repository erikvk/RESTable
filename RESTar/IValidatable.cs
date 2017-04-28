namespace RESTar
{
    public interface IValidatable
    {
        bool Validate(out string reason);
    }
}
