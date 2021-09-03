using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MultiClientMessaging.Server.Extensions
{
    public static class AspNetCoreExtensions
    {
        public static HubEndpointConventionBuilder MapMessagingHub(this IEndpointRouteBuilder endpoints, string path)
        {
            return endpoints.MapHub<ConnectionsHub>($"/{path}");
        }

        public static IServiceCollection AddMetaMessaging(this IServiceCollection services)
        {
            services.AddScoped<IConnectionsService, ConnectionsService>();
            return services;
        }
    }
}
