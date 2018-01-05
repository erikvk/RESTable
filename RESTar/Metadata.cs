using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Auth;
using RESTar.Internal;
using static RESTar.Methods;

namespace RESTar
{
    [RESTar(GET)]
    internal class Metadata : ISelector<Metadata>
    {
        public AccessRights CurrentAccessRights { get; private set; }
        public List<IResource> Resources { get; private set; }

        public IEnumerable<Metadata> Select(IRequest<Metadata> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var rights = RESTarConfig.AuthTokens[request.AuthToken];
            return new[]
            {
                new Metadata
                {
                    CurrentAccessRights = rights,
                    Resources = rights?
                        .Keys
                        .Where(r => r.IsGlobal && !r.IsInnerResource)
                        .Where(r => rights.ContainsKey(r))
                        .OrderBy(r => r.FullName)
                        .ToList()
                }
            };
        }
    }
}