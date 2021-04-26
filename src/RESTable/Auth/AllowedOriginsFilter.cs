using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace RESTable.Auth
{
    public class AllowedOriginsFilter : IAllowedOriginsFilter
    {
        private HashSet<Uri> Store { get; }

        private IOptionsSnapshot<AllowedOrigins> AllowedOriginsConfiguration { get; }

        public AllowedOriginsFilter(IOptionsSnapshot<AllowedOrigins> allowedOriginsConfiguration)
        {
            Store = new HashSet<Uri>();
            AllowedOriginsConfiguration = allowedOriginsConfiguration;
            foreach (var allowedOrigin in allowedOriginsConfiguration.Value)
            {
                if (string.IsNullOrWhiteSpace(allowedOrigin))
                    return;
                Store.Add(new Uri(allowedOrigin));
            }
        }

        public bool IsAllowed(Uri uri) => Store.Contains(uri);
    }
}