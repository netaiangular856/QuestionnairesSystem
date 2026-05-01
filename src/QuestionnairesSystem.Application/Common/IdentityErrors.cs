namespace QuestionnairesSystem.Application.Common;

public static class IdentityErrors
{
    public const string UserNotFound = "Identity.UserNotFound";

    public const string RoleNotFound = "Identity.RoleNotFound";

    public const string PermissionNotFound = "Identity.PermissionNotFound";

    public const string DuplicateUserName = "Identity.DuplicateUserName";

    public const string DuplicateEmail = "Identity.DuplicateEmail";

    public const string DuplicateRoleName = "Identity.DuplicateRoleName";

    public const string SystemRoleCannotDelete = "Identity.SystemRoleCannotDelete";

    public const string SystemRoleNameImmutable = "Identity.SystemRoleNameImmutable";

    public const string InvalidCredentials = "Identity.InvalidCredentials";

    public const string UserInactive = "Identity.UserInactive";

    public const string EmployeeNotFound = "Identity.EmployeeNotFound";

    public const string EmployeeAlreadyLinked = "Identity.EmployeeAlreadyLinked";
}

