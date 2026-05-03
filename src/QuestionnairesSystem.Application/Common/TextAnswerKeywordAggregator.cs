using System.Text.Json;
using System.Text.RegularExpressions;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Common;

/// <summary>Shared token counts for ShortText/LongText answers (survey or cross-survey scope).</summary>
public static class TextAnswerKeywordAggregator
{
    private static readonly HashSet<string> StopWords = new(
        new[]
        {
            "the", "and", "or", "is", "in", "to", "of", "a", "an", "as", "at", "be", "by", "it", "on", "if", "for",
            "في", "من", "على", "إلى", "عن", "مع", "هذا", "هذه", "ذلك", "التي", "الذي", "الى", "لا", "ما", "لم",
            "قد", "كل", "أن", "أو", "هل", "غير", "بعد", "قبل", "منذ", "عند", "هنا", "هناك",
        },
        StringComparer.Ordinal);

    private static readonly Regex WordTokenRegex = new(@"[\p{L}\p{Nd}]{2,40}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string UnwrapJsonAnswerText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var t = raw.Trim();
        try
        {
            return JsonSerializer.Deserialize<string>(t) ?? t;
        }
        catch
        {
            return t;
        }
    }

    public static IReadOnlyList<KeywordCountDto> Aggregate(IReadOnlyList<string> corpus)
    {
        if (corpus.Count == 0)
        {
            return Array.Empty<KeywordCountDto>();
        }

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var paragraph in corpus)
        {
            foreach (Match m in WordTokenRegex.Matches(paragraph))
            {
                var w = m.Value.Trim();
                if (w.Length < 2) continue;
                if (StopWords.Contains(w)) continue;
                if (w.All(char.IsDigit)) continue;
                freq.TryGetValue(w, out var n);
                freq[w] = n + 1;
            }
        }

        return freq
            .OrderByDescending(kv => kv.Value)
            .Take(24)
            .Select(kv => new KeywordCountDto { Keyword = kv.Key, Count = kv.Value })
            .ToList();
    }
}
