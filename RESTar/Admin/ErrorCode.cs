using System.Collections.Generic;
using System.Linq;
using RESTar.Deflection;
using RESTar.Internal;
using RESTar.Linq;
using static RESTar.Methods;

namespace RESTar.Admin
{
    /// <summary>
    /// Gets all error codes used by RESTar
    /// </summary>
    [RESTar(GET)]
    public class ErrorCode : ISelector<ErrorCode>
    {
        /// <summary>
        /// The name of the error
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The numeric code of the error
        /// </summary>
        public int Code { get; private set; }

        /// <summary>
        /// RESTar selector (don't use)
        /// </summary>
        public IEnumerable<ErrorCode> Select(IRequest<ErrorCode> request) => EnumMember<ErrorCodes>
            .GetMembers()
            .Select(m => new ErrorCode {Name = m.Name, Code = m.Value})
            .Where(request.Conditions);
    }
}