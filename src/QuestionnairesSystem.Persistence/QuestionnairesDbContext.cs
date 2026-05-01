using System.Reflection;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Domain.ActionPlans;
using QuestionnairesSystem.Domain.Audit;
using QuestionnairesSystem.Domain.Common;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Identity;
using QuestionnairesSystem.Domain.Lookups;
using QuestionnairesSystem.Domain.Notifications;
using QuestionnairesSystem.Domain.Organizations;
using QuestionnairesSystem.Domain.Participants;
using QuestionnairesSystem.Domain.Recommendations;
using QuestionnairesSystem.Domain.Responses;
using QuestionnairesSystem.Domain.Settings;
using QuestionnairesSystem.Domain.Surveys;
using QuestionnairesSystem.Domain.Templates;
using QuestionnairesSystem.Shared.Abstractions;
using QuestionnairesSystem.Shared.Identity;

namespace QuestionnairesSystem.Persistence;

public sealed class QuestionnairesDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public QuestionnairesDbContext(DbContextOptions<QuestionnairesDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyAudienceMember> SurveyAudienceMembers => Set<SurveyAudienceMember>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<SurveyTemplate> SurveyTemplates => Set<SurveyTemplate>();
    public DbSet<SurveyParticipant> SurveyParticipants => Set<SurveyParticipant>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<ActionPlan> ActionPlans => Set<ActionPlan>();
    public DbSet<Initiative> Initiatives => Set<Initiative>();
    public DbSet<InitiativeProgress> InitiativeProgressEntries => Set<InitiativeProgress>();
    public DbSet<InboxNotification> InboxNotifications => Set<InboxNotification>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<LookupItem> LookupItems => Set<LookupItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Department> Departments => Set<Department>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuestionnairesDbContext).Assembly);
        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.Id == Guid.Empty)
            {
                entry.Entity.Id = Guid.CreateVersion7();
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedOnUtc = utcNow;
                    entry.Entity.CreatedByUserId = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedOnUtc = utcNow;
                    entry.Entity.ModifiedByUserId = userId;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableDomainEntity>())
        {
            if (entry.State == EntityState.Modified &&
                entry.Entity.RecordStatus == RecordStatus.Deleted &&
                entry.Entity.DeletedOnUtc is null)
            {
                entry.Entity.DeletedOnUtc = utcNow;
                entry.Entity.DeletedByUserId = userId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (clrType is null || !typeof(AuditableDomainEntity).IsAssignableFrom(clrType))
            {
                continue;
            }

            var method = typeof(QuestionnairesDbContext)
                .GetMethod(nameof(SetSoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(clrType);

            method.Invoke(null, new object[] { modelBuilder });
        }
    }

    private static void SetSoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : AuditableDomainEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.RecordStatus != RecordStatus.Deleted);
    }
}
