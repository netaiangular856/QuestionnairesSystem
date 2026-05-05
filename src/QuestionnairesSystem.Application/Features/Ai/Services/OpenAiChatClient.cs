using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Application.Features.Ai;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.Ai.Services;

/// <summary>Minimal OpenAI Chat Completions client (no extra NuGet).</summary>
public sealed class OpenAiChatClient
{
    private static readonly JsonSerializerOptions RequestJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly IOptionsMonitor<OpenAiOptions> _options;
    private readonly IConfiguration _configuration;

    public OpenAiChatClient(HttpClient http, IOptionsMonitor<OpenAiOptions> options, IConfiguration configuration)
    {
        _http = http;
        _options = options;
        _configuration = configuration;
        if (_http.BaseAddress == null)
            _http.BaseAddress = new Uri("https://api.openai.com/v1/");
        _http.Timeout = TimeSpan.FromMinutes(3);
    }

    public async Task<Result<string>> CompleteAsync(
        IReadOnlyList<(string role, string content)> messages,
        bool jsonObjectFormat,
        CancellationToken cancellationToken)
    {
        var opt = _options.CurrentValue;
        var apiKey = OpenAiApiKeyResolver.Resolve(opt, _configuration);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Result<string>.Fail(
                "AI is not configured (missing API key).",
                QuestionnaireErrors.AiNotConfigured);
        }

        var model = string.IsNullOrWhiteSpace(opt.Model) ? "gpt-4o-mini" : opt.Model.Trim();
        var dto = new ChatCompletionRequestDto
        {
            Model = model,
            Temperature = 0.35,
            Messages = messages.Select(m => new ChatMessageDto { Role = m.role, Content = m.content }).ToList(),
            ResponseFormat = jsonObjectFormat ? new ResponseFormatDto { Type = "json_object" } : null,
        };

        var json = JsonSerializer.Serialize(dto, RequestJson);
        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage res;
        try
        {
            res = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"AI request failed: {ex.Message}",
                QuestionnaireErrors.AiProviderError);
        }

        var responseText = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;

        if (!res.IsSuccessStatusCode)
        {
            var errMsg = root.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var m)
                ? m.GetString()
                : res.ReasonPhrase;
            return Result<string>.Fail(
                errMsg ?? "OpenAI API error.",
                QuestionnaireErrors.AiProviderError);
        }

        try
        {
            var content = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content))
            {
                return Result<string>.Fail("Empty AI response.", QuestionnaireErrors.AiProviderError);
            }

            return Result<string>.Ok(content);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(
                $"Could not parse AI response: {ex.Message}",
                QuestionnaireErrors.AiProviderError);
        }
    }

    private sealed class ChatCompletionRequestDto
    {
        public string Model { get; set; } = "";
        public double Temperature { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = [];
        public ResponseFormatDto? ResponseFormat { get; set; }
    }

    private sealed class ChatMessageDto
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private sealed class ResponseFormatDto
    {
        public string Type { get; set; } = "";
    }
}
