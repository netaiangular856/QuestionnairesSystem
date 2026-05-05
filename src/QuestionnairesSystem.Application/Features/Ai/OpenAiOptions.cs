namespace QuestionnairesSystem.Application.Features.Ai;

public sealed class OpenAiOptions
{
    /// <summary>OpenAI API key (also bridged from env OPENAI_API_KEY_Q_AI in host).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat completion model id.</summary>
    public string Model { get; set; } = "gpt-4o-mini";
}
