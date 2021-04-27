using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace RESTable.Auth
{
    public class AllowedCorsOriginsFilter : IAllowedCorsOriginsFilter, IDisposable
    {
        private HashSet<Uri> Store { get; }
        private IDisposable ReloadToken { get; }

        public AllowedCorsOriginsFilter(IConfiguration configuration)
        {
            ReloadToken = configuration
                .GetReloadToken()
                .RegisterChangeCallback(state => Reload((IConfiguration) state), configuration);
            Store = new HashSet<Uri>();
            Reload(configuration);
        }

        private void Reload(IConfiguration configuration)
        {
            var allowedOrigins = configuration.Get<AllowedCorsOrigins>();
            if (allowedOrigins?.Count is not > 0)
                throw new InvalidOperationException($"When using {nameof(AllowedCorsOriginsFilter)}, the application configuration file is used " +
                                                    "to read allowed cors origins. The config file is missing an 'AllowedCorsOrigins' array " +
                                                    "with at least one string item.");
            foreach (var allowedOrigin in allowedOrigins)
            {
                if (string.IsNullOrWhiteSpace(allowedOrigin))
                    continue;
                Store.Add(new Uri(allowedOrigin));
            }
        }

        public bool IsAllowed(Uri uri) => Store.Contains(uri);

        public void Dispose() => ReloadToken.Dispose();
    }
}