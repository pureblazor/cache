using System.Runtime.Versioning;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

namespace CodeFrog.BlazorCache;

public record BlazorCacheOptions{}

public static class BlazorCacheExtensions
{
    public static IServiceCollection AddBlazorCache(this IServiceCollection services, Action<BlazorCacheOptions>? setupAction = null)
    {
        services.AddOptions();
        services.AddLocalStorageServices();
        services.Add(ServiceDescriptor.Singleton<IDistributedCache, LocalStorageCache>());

        if (setupAction is not null)
        {
            services.Configure(setupAction);
        }

        return services;
    }
}
