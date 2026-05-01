using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace QuestionnairesSystem.Persistence.DependencyInjection;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("QuestionnairesDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'QuestionnairesDb' is not configured.");
        }

        services.AddDbContext<QuestionnairesDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(QuestionnairesDbContext).Assembly.GetName().Name);
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        return services;
    }
}

