using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace RESTable.Auth
{
    public class AllowedCorsOriginsFilter : IAllowedCorsOriginsFilter, IDisposable
    {
        private HashSet<Uri> Store { get; }
        private IConfiguration Configuration { get; }
        private IDisposable ReloadToken { get; }

        public AllowedCorsOriginsFilter(IConfiguration configuration)
        {
            Configuration = configuration;
            Store = new HashSet<Uri>();
            Reload();
            ReloadToken = ChangeToken.OnChange(Configuration.GetReloadToken, Reload);
        }

        private void Reload()
        {
            var allowedOrigins = Configuration.GetSection(nameof(AllowedCorsOrigins)).Get<AllowedCorsOrigins>();
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