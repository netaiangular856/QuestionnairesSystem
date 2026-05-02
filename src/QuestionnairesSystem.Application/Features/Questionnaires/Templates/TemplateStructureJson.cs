using System.Text;
using System.Text.Json;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Templates;

internal static class TemplateStructureJson
{
    public static string FromQuestions(IReadOnlyList<CreateSurveyQuestionItem> questions)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteStartArray("questions");
            var i = 0;
            foreach (var q in questions)
            {
                i++;
                writer.WriteStartObject();
                writer.WriteString("titleAr", q.TitleAr.Trim());
                writer.WriteString("titleEn", q.TitleEn.Trim());
                writer.WriteNumber("type", (int)q.Type);
                writer.WriteNumber("displayOrder", q.DisplayOrder ?? i);
                if (!string.IsNullOrWhiteSpace(q.HelpTextAr))
                    writer.WriteString("helpTextAr", q.HelpTextAr.Trim());
                if (!string.IsNullOrWhiteSpace(q.HelpTextEn))
                    writer.WriteString("helpTextEn", q.HelpTextEn.Trim());
                writer.WriteBoolean("isRequired", q.IsRequired);
                if (!string.IsNullOrWhiteSpace(q.OptionsJson))
                {
                    using var opts = JsonDocument.Parse(q.OptionsJson);
                    writer.WritePropertyName("optionsJson");
                    opts.RootElement.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static IReadOnlyList<CreateSurveyQuestionItem> ParseQuestions(string? structureJson)
    {
        var list = new List<CreateSurveyQuestionItem>();
        if (string.IsNullOrWhiteSpace(structureJson))
            return list;

        try
        {
            using var doc = JsonDocument.Parse(structureJson);
            if (!doc.RootElement.TryGetProperty("questions", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var el in arr.EnumerateArray())
            {
                var titleAr = el.TryGetProperty("titleAr", out var tar) ? tar.GetString() ?? string.Empty : string.Empty;
                var titleEn = el.TryGetProperty("titleEn", out var ten) ? ten.GetString() ?? string.Empty : string.Empty;
                var typeVal = QuestionType.ShortText;
                if (el.TryGetProperty("type", out var tv) && tv.ValueKind == JsonValueKind.Number && tv.TryGetInt32(out var ti))
                    typeVal = (QuestionType)ti;

                string? optionsJson = null;
                if (el.TryGetProperty("optionsJson", out var oj))
                    optionsJson = oj.GetRawText();

                list.Add(new CreateSurveyQuestionItem
                {
                    Type = typeVal,
                    TitleAr = titleAr,
                    TitleEn = titleEn,
                    HelpTextAr = el.TryGetProperty("helpTextAr", out var ha) ? ha.GetString() : null,
                    HelpTextEn = el.TryGetProperty("helpTextEn", out var he) ? he.GetString() : null,
                    IsRequired = el.TryGetProperty("isRequired", out var ir) && ir.ValueKind == JsonValueKind.True,
                    OptionsJson = optionsJson,
                    DisplayOrder = el.TryGetProperty("displayOrder", out var d) && d.TryGetInt32(out var di) ? di : null
                });
            }
        }
        catch
        {
            /* ignore malformed */
        }

        return list;
    }

    public static int CountQuestions(string? structureJson)
    {
        if (string.IsNullOrWhiteSpace(structureJson))
            return 0;
        try
        {
            using var doc = JsonDocument.Parse(structureJson);
            if (!doc.RootElement.TryGetProperty("questions", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return 0;
            return arr.GetArrayLength();
        }
        catch
        {
            return 0;
        }
    }
}
