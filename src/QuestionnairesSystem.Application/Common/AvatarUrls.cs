namespace QuestionnairesSystem.Application.Common;

public static class AvatarUrls
{
    public static string? ToPublicUrl(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? null : $"/uploads/avatars/{fileName.Trim()}";
}
