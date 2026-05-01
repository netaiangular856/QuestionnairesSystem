using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using QuestionnairesSystem.Shared.Identity;

namespace QuestionnairesSystem.Persistence;

/// <summary>
/// يُستخدم من أدوات EF فقط لتوليد الهجرات دون تشغيل مشروع الـ API.
/// </summary>
public sealed class QuestionnairesDbContextFactory : IDesignTimeDbContextFactory<QuestionnairesDbContext>
{
    public QuestionnairesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<QuestionnairesDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=QuestionnairesSystem_Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

        return new QuestionnairesDbContext(optionsBuilder.Options, new DesignTimeCurrentUserService());
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => null;

        public string? UserName => null;

        public bool IsAuthenticated => false;
    }
}
