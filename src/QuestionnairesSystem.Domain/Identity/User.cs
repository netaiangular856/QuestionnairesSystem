using QuestionnairesSystem.Domain.Notifications;
using QuestionnairesSystem.Domain.Organizations;
using QuestionnairesSystem.Shared.Abstractions;

namespace QuestionnairesSystem.Domain.Identity;

public sealed class User : AuditableEntity
{
    public string UserName { get; set; } = null!;
    public string? NameAr { get; set; }
    public string? NameEn { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public Guid? EmployeeId { get; set; }
    public DateTime? LastLoginUtc { get; set; }

    public Employee? Employee { get; set; }

    /// <summary>Stored file name under wwwroot/uploads/avatars (e.g. guid.jpg). Null if no avatar.</summary>
    public string? AvatarFileName { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<InboxNotification> InboxNotifications { get; set; } = new List<InboxNotification>();
}
