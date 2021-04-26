using System.Threading.Tasks;
using RESTable.Meta;
using RESTable.Requests;
using RESTable.Results;

namespace RESTable.Auth
{
    public class ResourceAuthenticator
    {
        public async Task ResourceAuthenticate<T>(IRequest<T> request, IEntityResource<T> resource) where T : class
        {
            if (request.Context.Client.ResourceAuthMappings.ContainsKey(resource))
                return;
            var authResults = await resource.AuthenticateAsync(request).ConfigureAwait(false);
            if (authResults.Success)
                request.Context.Client.ResourceAuthMappings[resource] = default;
            else throw new FailedResourceAuthentication(authResults.FailedReason);
        }
    }
}