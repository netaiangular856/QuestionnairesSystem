namespace QuestionnairesSystem.Shared.Options;

public sealed class IdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";

    /// <summary>When false, startup does not run RBAC seeding (useful for integration tests without a database).</summary>
    public bool RunOnStartup { get; set; } = true;

    public bool BootstrapAdmin { get; set; }

    public string AdminEmail { get; set; } = "admin@localhost";

    public string AdminUserName { get; set; } = "admin";

    public string AdminPassword { get; set; } = "ChangeMe!1";
}

