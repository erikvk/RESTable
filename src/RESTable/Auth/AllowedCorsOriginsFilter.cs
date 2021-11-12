using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace RESTable.Auth
{
    public class AllowedCorsOriginsFilter : IAllowedCorsOriginsFilter
    {
        private HashSet<Uri> Store { get; }
        private IOptionsMonitor<AllowedCorsOrigins> Config { get; }

        public AllowedCorsOriginsFilter(IOptionsMonitor<AllowedCorsOrigins> config)
        {
            Config = config;
            Store = new HashSet<Uri>();
            Reload(config.CurrentValue);
            config.OnChange(Reload);
        }

        private void Reload(AllowedCorsOrigins allowedOrigins)
        {
            if (allowedOrigins.Count <= 0)
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
    }
}