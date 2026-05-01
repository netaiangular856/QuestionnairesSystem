namespace QuestionnairesSystem.Shared.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "QuestionnairesSystem";

    public string Audience { get; set; } = "QuestionnairesSystem";

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 60;
}

