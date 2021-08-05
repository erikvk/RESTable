using RESTable.Requests;

namespace RESTable.Results
{
    internal class ShellSuccess : Success
    {
        public sealed override IRequest Request { get; }

        public ShellSuccess(IProtocolHolder protocolHolder) : base(protocolHolder)
        {
            Request = null!;
        }
    }
}