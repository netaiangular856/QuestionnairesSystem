using System.Globalization;
using System.Text.Json;
using QuestionnairesSystem.Application.Common;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Export;

/// <summary>Human-readable answer text for survey exports (Arabic + English labels).</summary>
public static class CrossSurveyAnswerDetailFormatter
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static (string AnswerAr, string AnswerEn) FormatAnswer(QuestionType type, string? optionsJson, string? valueJson)
    {
        return type switch
        {
            QuestionType.ShortText or QuestionType.LongText => FormatText(valueJson),
            QuestionType.Rating or QuestionType.Scale => FormatNumeric(valueJson),
            QuestionType.SingleChoice => FormatSingleChoice(optionsJson, valueJson),
            QuestionType.MultipleChoice => FormatMultipleChoice(optionsJson, valueJson),
            QuestionType.YesNo => FormatYesNo(valueJson),
            QuestionType.Date => FormatDate(valueJson),
            QuestionType.Number => FormatNumber(valueJson),
            _ => (valueJson ?? string.Empty, valueJson ?? string.Empty),
        };
    }

    private static (string, string) FormatText(string? valueJson)
    {
        var t = TextAnswerKeywordAggregator.UnwrapJsonAnswerText(valueJson);
        return (t, t);
    }

    private static (string, string) FormatNumeric(string? valueJson)
    {
        if (TryParseDouble(valueJson, out var d))
        {
            var s = d.ToString("0.###", Invariant);
            return (s, s);
        }

        var raw = valueJson?.Trim() ?? string.Empty;
        return (raw, raw);
    }

    private static (string, string) FormatSingleChoice(string? optionsJson, string? valueJson)
    {
        var map = ParseChoiceOptionsMap(optionsJson);
        var val = TryDeserializeChoiceValue(valueJson);
        if (string.IsNullOrEmpty(val))
        {
            return (string.Empty, string.Empty);
        }

        if (map.TryGetValue(val, out var labels))
        {
            var ar = string.IsNullOrWhiteSpace(labels.Ar) ? val : labels.Ar;
            var en = string.IsNullOrWhiteSpace(labels.En) ? val : labels.En;
            return (ar, en);
        }

        return (val, val);
    }

    private static (string, string) FormatMultipleChoice(string? optionsJson, string? valueJson)
    {
        var map = ParseChoiceOptionsMap(optionsJson);
        var keys = TryDeserializeChoiceArray(valueJson);
        if (keys.Count == 0)
        {
            return (string.Empty, string.Empty);
        }

        var arParts = new List<string>();
        var enParts = new List<string>();
        foreach (var val in keys)
        {
            if (string.IsNullOrEmpty(val))
            {
                continue;
            }

            if (map.TryGetValue(val, out var labels))
            {
                arParts.Add(string.IsNullOrWhiteSpace(labels.Ar) ? val : labels.Ar);
                enParts.Add(string.IsNullOrWhiteSpace(labels.En) ? val : labels.En);
            }
            else
            {
                arParts.Add(val);
                enParts.Add(val);
            }
        }

        return (string.Join("، ", arParts), string.Join(", ", enParts));
    }

    private static (string, string) FormatYesNo(string? valueJson)
    {
        if (!TryParseBool(valueJson, out var b))
        {
            var raw = TextAnswerKeywordAggregator.UnwrapJsonAnswerText(valueJson);
            return (raw, raw);
        }

        return b ? ("نعم", "Yes") : ("لا", "No");
    }

    private static (string, string) FormatDate(string? valueJson)
    {
        var raw = TextAnswerKeywordAggregator.UnwrapJsonAnswerText(valueJson).Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return (string.Empty, string.Empty);
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt) ||
            DateTime.TryParse(raw, out dt))
        {
            var s = dt.Date.ToString("yyyy-MM-dd", Invariant);
            return (s, s);
        }

        return (raw, raw);
    }

    private static (string, string) FormatNumber(string? valueJson)
    {
        if (TryParseDouble(valueJson, out var d))
        {
            var s = d % 1 == 0 ? d.ToString("0", Invariant) : d.ToString("0.###", Invariant);
            return (s, s);
        }

        var raw = TextAnswerKeywordAggregator.UnwrapJsonAnswerText(valueJson);
        return (raw, raw);
    }

    private static bool TryParseBool(string? raw, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var t = raw.Trim();
        if (bool.TryParse(t, out value))
        {
            return true;
        }

        if (t.Equals("\"true\"", StringComparison.OrdinalIgnoreCase) || t.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (t.Equals("\"false\"", StringComparison.OrdinalIgnoreCase) || t.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(t);
            if (doc.RootElement.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    private static bool TryParseDouble(string? raw, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var t = raw.Trim();
        if (double.TryParse(t, NumberStyles.Any, Invariant, out value))
        {
            return true;
        }

        try
        {
            var s = JsonSerializer.Deserialize<string>(t);
            if (!string.IsNullOrWhiteSpace(s) &&
                double.TryParse(s.Trim(), NumberStyles.Any, Invariant, out value))
            {
                return true;
            }
        }
        catch
        {
            // ignore
        }

        try
        {
            value = JsonSerializer.Deserialize<double>(t);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Dictionary<string, (string Ar, string En)> ParseChoiceOptionsMap(string? optionsJson)
    {
        var map = new Dictionary<string, (string Ar, string En)>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(optionsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (!el.TryGetProperty("value", out var vEl))
                {
                    continue;
                }

                var v = vEl.GetString();
                if (string.IsNullOrEmpty(v))
                {
                    continue;
                }

                var ar = el.TryGetProperty("labelAr", out var la) ? la.GetString() ?? string.Empty : string.Empty;
                var en = el.TryGetProperty("labelEn", out var le) ? le.GetString() ?? string.Empty : string.Empty;
                map[v] = (ar, en);
            }
        }
        catch
        {
            /* ignore malformed options */
        }

        return map;
    }

    private static string? TryDeserializeChoiceValue(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var t = json.Trim();
        try
        {
            if (t.StartsWith('['))
            {
                return null;
            }

            return JsonSerializer.Deserialize<string>(json);
        }
        catch
        {
            return t.Trim('"');
        }
    }

    private static IReadOnlyList<string> TryDeserializeChoiceArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        var t = json.Trim();
        try
        {
            if (!t.StartsWith('['))
            {
                return Array.Empty<string>();
            }

            var arr = JsonSerializer.Deserialize<string[]>(json);
            return arr ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
