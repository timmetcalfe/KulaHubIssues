using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KulaHub.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKulaHubData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KulaHubDatabase")
            ?? configuration["KULAHUB_DATABASE_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A database connection string named 'KulaHubDatabase' or the KULAHUB_DATABASE_CONNECTION_STRING setting is required.");
        }

        services.AddDbContext<KulaHubDbContext>(options =>
        {
            if (connectionString.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(connectionString);
            }
            else
            {
                options.UseSqlServer(connectionString);
            }
        });

        services.AddScoped<IKulaHubCrmService, KulaHubCrmService>();

        return services;
    }
}