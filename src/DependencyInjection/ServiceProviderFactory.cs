using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace SwiftlyS2_Deathmatch.DependencyInjection;

public static class ServiceProviderFactory
{
    public static IServiceProvider CreateServiceProvider(ISwiftlyCore core, ILogger logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton(core);
        services.AddSingleton(logger);
        services.AddDeathmatchServices();
        return services.BuildServiceProvider();
    }

    public static void DisposeServiceProvider(IServiceProvider? serviceProvider)
    {
        if (serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
