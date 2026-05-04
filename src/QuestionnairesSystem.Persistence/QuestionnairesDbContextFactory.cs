using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using QuestionnairesSystem.Shared.Identity;

namespace QuestionnairesSystem.Persistence;

/// <summary>
/// يُستخدم من أدوات EF فقط لتوليد الهجرات وتطبيقها.
/// يقرأ سلسلة الاتصال من QuestionnairesSystem.Api (appsettings + ASPNETCORE_ENVIRONMENT).
/// </summary>
public sealed class QuestionnairesDbContextFactory : IDesignTimeDbContextFactory<QuestionnairesDbContext>
{
    public QuestionnairesDbContext CreateDbContext(string[] args)
    {
        var contentRoot = ResolveApiContentRoot();
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("QuestionnairesDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'QuestionnairesDb' is missing. Check QuestionnairesSystem.Api appsettings for the current environment.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<QuestionnairesDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(QuestionnairesDbContext).Assembly.GetName().Name);
        });

        return new QuestionnairesDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    private static string ResolveApiContentRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var direct = Path.Combine(dir.FullName, "QuestionnairesSystem.Api");
            if (File.Exists(Path.Combine(direct, "appsettings.json")))
            {
                return direct;
            }

            var underSrc = Path.Combine(dir.FullName, "src", "QuestionnairesSystem.Api");
            if (File.Exists(Path.Combine(underSrc, "appsettings.json")))
            {
                return underSrc;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not find QuestionnairesSystem.Api/appsettings.json. Run `dotnet ef` from the solution/repository root (or any parent folder that contains src/QuestionnairesSystem.Api).");
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;

        public string? UserName => null;

        public bool IsAuthenticated => false;
    }
}
