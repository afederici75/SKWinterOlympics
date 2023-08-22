using Microsoft.Extensions.Options;

namespace SKWinterOlympics;

public static class DependencyInjection
{
    public static IServiceCollection AddSKWinterOlympics(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IMemoryStore, VolatileMemoryStore>();
        services.AddSingleton<CsvLoader, CsvLoader>();
        services.Configure<SemanticKernelOptions>(
            configuration.GetSection("SemanticKernel"));

        return services;
    }
}
