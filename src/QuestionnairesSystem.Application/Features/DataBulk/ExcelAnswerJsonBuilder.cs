using System.Globalization;
using System.Text.Json;
using QuestionnairesSystem.Domain.Enums;

namespace QuestionnairesSystem.Application.Features.DataBulk;

internal static class ExcelAnswerJsonBuilder
{
    public static string Build(QuestionType type, string raw)
    {
        var t = raw?.Trim() ?? string.Empty;
        return type switch
        {
            QuestionType.ShortText or QuestionType.LongText => JsonSerializer.Serialize(t),
            QuestionType.Number or QuestionType.Rating or QuestionType.Scale => BuildNumberOrString(t),
            QuestionType.YesNo => JsonSerializer.Serialize(ParseYesNo(t)),
            QuestionType.Date => JsonSerializer.Serialize(t),
            QuestionType.SingleChoice => JsonSerializer.Serialize(t),
            QuestionType.MultipleChoice => BuildMulti(t),
            _ => JsonSerializer.Serialize(t),
        };
    }

    private static string BuildNumberOrString(string t)
    {
        if (string.IsNullOrEmpty(t))
        {
            return JsonSerializer.Serialize(string.Empty);
        }

        if (double.TryParse(t, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return JsonSerializer.Serialize(d);
        }

        return JsonSerializer.Serialize(t);
    }

    private static string BuildMulti(string t)
    {
        if (string.IsNullOrWhiteSpace(t))
        {
            return "[]";
        }

        var parts = t.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return JsonSerializer.Serialize(parts);
    }

    private static bool ParseYesNo(string t)
    {
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        var s = t.Trim();
        if (bool.TryParse(s, out var b))
        {
            return b;
        }

        if (s.Equals("1", StringComparison.Ordinal) || s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("y", StringComparison.OrdinalIgnoreCase) || s.Equals("نعم", StringComparison.Ordinal))
        {
            return true;
        }

        if (s.Equals("0", StringComparison.Ordinal) || s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("n", StringComparison.OrdinalIgnoreCase) || s.Equals("لا", StringComparison.Ordinal))
        {
            return false;
        }

        return false;
    }
}
