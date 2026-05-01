namespace QuestionnairesSystem.Application.Common;

/// <summary>Human-readable user label: Arabic name, then English, then login.</summary>
public static class UserDisplayNames
{
    public static string? Format(string? nameAr, string? nameEn, string? userName)
    {
        if (!string.IsNullOrWhiteSpace(nameAr))
            return nameAr.Trim();
        if (!string.IsNullOrWhiteSpace(nameEn))
            return nameEn.Trim();
        if (!string.IsNullOrWhiteSpace(userName))
            return userName.Trim();
        return null;
    }
}
