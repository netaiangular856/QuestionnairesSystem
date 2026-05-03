namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Export;

/// <summary>UI strings for PDF/Excel analytics export (ar / en).</summary>
public static class CrossSurveyAnalyticsReportLocalization
{
    public static string NormalizeLang(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return "en";
        }

        var l = lang.Trim().ToLowerInvariant();
        return l.StartsWith("ar", StringComparison.Ordinal) ? "ar" : "en";
    }

    public static bool IsArabic(string lang) => lang == "ar";

    /// <summary>Prefer Arabic when lang=ar, English when lang=en; fallback to the other if empty.</summary>
    public static string PickSurveyTitle(string lang, string? titleAr, string? titleEn)
    {
        var ar = titleAr?.Trim() ?? string.Empty;
        var en = titleEn?.Trim() ?? string.Empty;
        if (IsArabic(lang))
        {
            return string.IsNullOrEmpty(ar) ? en : ar;
        }

        return string.IsNullOrEmpty(en) ? ar : en;
    }

    public static string Message(string lang, ReportMessageId id)
    {
        var ar = MessagesAr.TryGetValue(id, out var a) ? a : id.ToString();
        var en = MessagesEn.TryGetValue(id, out var e) ? e : id.ToString();
        return IsArabic(lang) ? ar : en;
    }

    public static string TranslateSurveyStatus(string lang, string key) =>
        TryTranslate(lang, SurveyStatusMap, key);

    public static string TranslateResponseStatus(string lang, string key) =>
        TryTranslate(lang, ResponseStatusMap, key);

    public static string TranslateParticipantStatus(string lang, string key) =>
        TryTranslate(lang, ParticipantStatusMap, key);

    public static string TranslateAudienceScope(string lang, string key) =>
        TryTranslate(lang, AudienceScopeMap, key);

    public static string TranslateQuestionType(string lang, string key) =>
        TryTranslate(lang, QuestionTypeMap, key);

    public static string TranslateDayOfWeek(string lang, string key) =>
        TryTranslate(lang, DayOfWeekMap, key);

    private static string TryTranslate(string lang, IReadOnlyDictionary<string, (string Ar, string En)> map, string key)
    {
        if (!map.TryGetValue(key, out var t))
        {
            return key;
        }

        return IsArabic(lang) ? t.Ar : t.En;
    }

    private static readonly Dictionary<ReportMessageId, string> MessagesAr = new()
    {
        [ReportMessageId.ReportTitle] = "تقرير تحليل الاستبيانات",
        [ReportMessageId.GeneratedUtc] = "تاريخ الإنشاء (UTC)",
        [ReportMessageId.AppliedScope] = "نطاق التقرير",
        [ReportMessageId.SurveyScope] = "الاستبيان",
        [ReportMessageId.AllSurveys] = "كل الاستبيانات",
        [ReportMessageId.FromUtc] = "من (UTC)",
        [ReportMessageId.ToUtc] = "إلى (UTC)",
        [ReportMessageId.Overview] = "نظرة عامة",
        [ReportMessageId.Metric] = "المؤشر",
        [ReportMessageId.Value] = "القيمة",
        [ReportMessageId.Distributions] = "التوزيعات",
        [ReportMessageId.SurveyStatus] = "حالة الاستبيان",
        [ReportMessageId.ResponseStatus] = "حالة الرد",
        [ReportMessageId.AudienceScope] = "نطاق الجمهور",
        [ReportMessageId.ParticipantStatus] = "حالة المدعو",
        [ReportMessageId.SubmissionsByWeekday] = "الإرسال حسب يوم الأسبوع",
        [ReportMessageId.Key] = "البند",
        [ReportMessageId.Count] = "العدد",
        [ReportMessageId.SubmissionsByDay] = "الإرسال اليومي",
        [ReportMessageId.DateUtc] = "التاريخ (UTC)",
        [ReportMessageId.TopSurveys] = "أبرز الاستبيانات حسب الإرسال",
        [ReportMessageId.SurveyName] = "اسم الاستبيان",
        [ReportMessageId.Status] = "الحالة",
        [ReportMessageId.Submissions] = "عدد الإرسال في الفترة",
        [ReportMessageId.RatingsSection] = "التقييمات والمقاييس (مجمّع)",
        [ReportMessageId.Rating] = "التقييم",
        [ReportMessageId.Percent] = "النسبة %",
        [ReportMessageId.QuestionTypes] = "الإجابات حسب نوع السؤال",
        [ReportMessageId.QuestionType] = "نوع السؤال",
        [ReportMessageId.AnswerCount] = "عدد الإجابات",
        [ReportMessageId.TextKeywords] = "أكثر الكلمات في الإجابات النصية (تكرار)",
        [ReportMessageId.Keyword] = "الكلمة",
        [ReportMessageId.KeywordFootnote] =
            "عدّ تكرار الكلمات من الإجابات النصية فقط — وليس تحليلاً بالذكاء الاصطناعي.",
        [ReportMessageId.NoDailyData] = "لا توجد إرسالات يومية ضمن هذا النطاق.",
        [ReportMessageId.NoTopSurveys] = "لا توجد استبيانات بإرسال ضمن هذا النطاق.",
        [ReportMessageId.NoRatings] = "لا توجد إجابات تقييم أو مقياس ضمن هذا النطاق.",
        [ReportMessageId.NoKeywords] = "لا توجد نصوص مستخرجة (لا إجابات نصية أو نص فارغ).",
        [ReportMessageId.EmptyDash] = "—",
        [ReportMessageId.FooterBrand] = "Questionnaires OS",
        [ReportMessageId.Page] = "صفحة",
        [ReportMessageId.Of] = "من",
        [ReportMessageId.WorkbookTitle] = "تصدير تحليل الاستبيانات",
        [ReportMessageId.SheetSummary] = "ملخص",
        [ReportMessageId.SheetBreakdowns] = "توزيعات",
        [ReportMessageId.SheetDaily] = "يومي",
        [ReportMessageId.SheetTopSurveys] = "أبرز الاستبيانات",
        [ReportMessageId.SheetRatings] = "التقييمات",
        [ReportMessageId.SheetQuestionTypes] = "أنواع الأسئلة",
        [ReportMessageId.SheetKeywords] = "كلمات نصية",
        [ReportMessageId.FilterSection] = "فلتر التقرير",
        [ReportMessageId.SurveyIdLabel] = "الاستبيان",
        [ReportMessageId.NoteKeywordsNotAi] = "ملاحظة: ملخص تكرار للكلمات وليس ذكاءً اصطناعياً.",
        [ReportMessageId.Percentage] = "النسبة المئوية",
        [ReportMessageId.Section] = "القسم",
        [ReportMessageId.DetailedAnswersSection] = "تفاصيل الإجابات (كل رد مُرسَل)",
        [ReportMessageId.SubmittedAtCol] = "وقت الإرسال (UTC)",
        [ReportMessageId.RespondentCol] = "المستجيب",
        [ReportMessageId.QuestionCol] = "السؤال",
        [ReportMessageId.AnswerCol] = "الإجابة",
        [ReportMessageId.SheetAnswers] = "الإجابات التفصيلية",
        [ReportMessageId.NoAnswerDetails] = "لا توجد إجابات في هذا النطاق.",
        [ReportMessageId.PdfAnswersTruncated] =
            "تم اقتصار جدول الإجابات في PDF على أول {0} صفًا. افتح ملف Excel لعرض كل الإجابات.",
    };

    private static readonly Dictionary<ReportMessageId, string> MessagesEn = new()
    {
        [ReportMessageId.ReportTitle] = "Survey analytics report",
        [ReportMessageId.GeneratedUtc] = "Generated (UTC)",
        [ReportMessageId.AppliedScope] = "Report scope",
        [ReportMessageId.SurveyScope] = "Survey",
        [ReportMessageId.AllSurveys] = "All surveys",
        [ReportMessageId.FromUtc] = "From (UTC)",
        [ReportMessageId.ToUtc] = "To (UTC)",
        [ReportMessageId.Overview] = "Overview",
        [ReportMessageId.Metric] = "Metric",
        [ReportMessageId.Value] = "Value",
        [ReportMessageId.Distributions] = "Distributions",
        [ReportMessageId.SurveyStatus] = "Survey status",
        [ReportMessageId.ResponseStatus] = "Response status",
        [ReportMessageId.AudienceScope] = "Audience scope",
        [ReportMessageId.ParticipantStatus] = "Participant status",
        [ReportMessageId.SubmissionsByWeekday] = "Submissions by weekday",
        [ReportMessageId.Key] = "Item",
        [ReportMessageId.Count] = "Count",
        [ReportMessageId.SubmissionsByDay] = "Daily submissions",
        [ReportMessageId.DateUtc] = "Date (UTC)",
        [ReportMessageId.TopSurveys] = "Top surveys by submissions",
        [ReportMessageId.SurveyName] = "Survey name",
        [ReportMessageId.Status] = "Status",
        [ReportMessageId.Submissions] = "Submissions in period",
        [ReportMessageId.RatingsSection] = "Ratings & scales (aggregated)",
        [ReportMessageId.Rating] = "Rating",
        [ReportMessageId.Percent] = "%",
        [ReportMessageId.QuestionTypes] = "Answers by question type",
        [ReportMessageId.QuestionType] = "Question type",
        [ReportMessageId.AnswerCount] = "Answer count",
        [ReportMessageId.TextKeywords] = "Text answer keywords (frequency)",
        [ReportMessageId.Keyword] = "Keyword",
        [ReportMessageId.KeywordFootnote] =
            "Keyword counts are frequency summaries from text answers — not AI-generated insights.",
        [ReportMessageId.NoDailyData] = "No daily submissions in this scope.",
        [ReportMessageId.NoTopSurveys] = "No surveys with submissions in this scope.",
        [ReportMessageId.NoRatings] = "No rating or scale answers in this scope.",
        [ReportMessageId.NoKeywords] = "No text extracted (no text answers or empty text).",
        [ReportMessageId.EmptyDash] = "—",
        [ReportMessageId.FooterBrand] = "Questionnaires OS",
        [ReportMessageId.Page] = "Page",
        [ReportMessageId.Of] = "of",
        [ReportMessageId.WorkbookTitle] = "Survey analytics export",
        [ReportMessageId.SheetSummary] = "Summary",
        [ReportMessageId.SheetBreakdowns] = "Breakdowns",
        [ReportMessageId.SheetDaily] = "Daily",
        [ReportMessageId.SheetTopSurveys] = "Top surveys",
        [ReportMessageId.SheetRatings] = "Ratings",
        [ReportMessageId.SheetQuestionTypes] = "Question types",
        [ReportMessageId.SheetKeywords] = "Text keywords",
        [ReportMessageId.FilterSection] = "Applied filter",
        [ReportMessageId.SurveyIdLabel] = "Survey",
        [ReportMessageId.NoteKeywordsNotAi] = "Note: token-frequency summary (not AI).",
        [ReportMessageId.Percentage] = "Percentage",
        [ReportMessageId.Section] = "Section",
        [ReportMessageId.DetailedAnswersSection] = "Answer details (every submitted response)",
        [ReportMessageId.SubmittedAtCol] = "Submitted at (UTC)",
        [ReportMessageId.RespondentCol] = "Respondent",
        [ReportMessageId.QuestionCol] = "Question",
        [ReportMessageId.AnswerCol] = "Answer",
        [ReportMessageId.SheetAnswers] = "Detailed answers",
        [ReportMessageId.NoAnswerDetails] = "No answers in this scope.",
        [ReportMessageId.PdfAnswersTruncated] =
            "The PDF answer table shows only the first {0} rows. Open the Excel file for the full dataset.",
    };

    private static readonly Dictionary<string, (string Ar, string En)> SurveyStatusMap = new(StringComparer.Ordinal)
    {
        ["Draft"] = ("مسودة", "Draft"),
        ["PendingApproval"] = ("بانتظار الاعتماد", "Pending approval"),
        ["Approved"] = ("معتمد", "Approved"),
        ["Published"] = ("منشور", "Published"),
        ["Closed"] = ("مغلق", "Closed"),
        ["Rejected"] = ("مرفوض", "Rejected"),
    };

    private static readonly Dictionary<string, (string Ar, string En)> ResponseStatusMap = new(StringComparer.Ordinal)
    {
        ["InProgress"] = ("قيد الإكمال", "In progress"),
        ["Submitted"] = ("مُرسَل", "Submitted"),
        ["Invalid"] = ("غير صالح", "Invalid"),
    };

    private static readonly Dictionary<string, (string Ar, string En)> ParticipantStatusMap = new(StringComparer.Ordinal)
    {
        ["Invited"] = ("مدعو", "Invited"),
        ["Started"] = ("بدأ", "Started"),
        ["Completed"] = ("أكمل", "Completed"),
        ["Declined"] = ("رفض", "Declined"),
    };

    private static readonly Dictionary<string, (string Ar, string En)> AudienceScopeMap = new(StringComparer.Ordinal)
    {
        ["Everyone"] = ("الجميع", "Everyone"),
        ["Guest"] = ("ضيف", "Guest"),
        ["SpecificUsers"] = ("مستخدمون محددون", "Specific users"),
        ["AllOrganizationMembers"] = ("كل أعضاء المنظمة", "All organization members"),
    };

    private static readonly Dictionary<string, (string Ar, string En)> QuestionTypeMap = new(StringComparer.Ordinal)
    {
        ["ShortText"] = ("نص قصير", "Short text"),
        ["LongText"] = ("نص طويل", "Long text"),
        ["SingleChoice"] = ("اختيار واحد", "Single choice"),
        ["MultipleChoice"] = ("متعدد الخيارات", "Multiple choice"),
        ["Rating"] = ("تقييم", "Rating"),
        ["Scale"] = ("مقياس", "Scale"),
        ["YesNo"] = ("نعم / لا", "Yes / No"),
        ["Date"] = ("تاريخ", "Date"),
        ["Number"] = ("رقم", "Number"),
    };

    private static readonly Dictionary<string, (string Ar, string En)> DayOfWeekMap = new(StringComparer.Ordinal)
    {
        ["Sunday"] = ("الأحد", "Sunday"),
        ["Monday"] = ("الإثنين", "Monday"),
        ["Tuesday"] = ("الثلاثاء", "Tuesday"),
        ["Wednesday"] = ("الأربعاء", "Wednesday"),
        ["Thursday"] = ("الخميس", "Thursday"),
        ["Friday"] = ("الجمعة", "Friday"),
        ["Saturday"] = ("السبت", "Saturday"),
    };

    private static readonly Dictionary<OverviewMetricId, string> OverviewMetricsAr = new()
    {
        [OverviewMetricId.SurveysInScope] = "استبيانات في النطاق",
        [OverviewMetricId.PublishedSurveys] = "منشورة",
        [OverviewMetricId.SubmittedResponses] = "ردود مُرسَلة (الفترة)",
        [OverviewMetricId.SurveysWithSubmissions] = "استبيانات بها ردود في الفترة",
        [OverviewMetricId.InvitedParticipants] = "مدعوون (مجموع)",
        [OverviewMetricId.ResponsesInProgress] = "ردود قيد الإكمال",
        [OverviewMetricId.AvgSubmissionsPerDay] = "متوسط إرسال يومي (الفترة)",
        [OverviewMetricId.QuestionsInScope] = "أسئلة في النطاق",
        [OverviewMetricId.ParticipantsCompleted] = "مشاركون أكملوا",
        [OverviewMetricId.ParticipantsDeclined] = "رفضوا المشاركة",
        [OverviewMetricId.AvgMinutesToSubmit] = "متوسط وقت الإكمال (دقيقة)",
    };

    private static readonly Dictionary<OverviewMetricId, string> OverviewMetricsEn = new()
    {
        [OverviewMetricId.SurveysInScope] = "Surveys in scope",
        [OverviewMetricId.PublishedSurveys] = "Published surveys",
        [OverviewMetricId.SubmittedResponses] = "Submitted responses (period)",
        [OverviewMetricId.SurveysWithSubmissions] = "Surveys with submissions (period)",
        [OverviewMetricId.InvitedParticipants] = "Invited participants",
        [OverviewMetricId.ResponsesInProgress] = "Responses in progress",
        [OverviewMetricId.AvgSubmissionsPerDay] = "Avg. submissions per day (period)",
        [OverviewMetricId.QuestionsInScope] = "Questions in scope",
        [OverviewMetricId.ParticipantsCompleted] = "Participants completed",
        [OverviewMetricId.ParticipantsDeclined] = "Participants declined",
        [OverviewMetricId.AvgMinutesToSubmit] = "Avg. minutes to submit (period)",
    };

    public static string OverviewMetric(string lang, OverviewMetricId id)
    {
        var ar = OverviewMetricsAr[id];
        var en = OverviewMetricsEn[id];
        return IsArabic(lang) ? ar : en;
    }
}

public enum ReportMessageId
{
    ReportTitle,
    GeneratedUtc,
    AppliedScope,
    SurveyScope,
    AllSurveys,
    FromUtc,
    ToUtc,
    Overview,
    Metric,
    Value,
    Distributions,
    SurveyStatus,
    ResponseStatus,
    AudienceScope,
    ParticipantStatus,
    SubmissionsByWeekday,
    Key,
    Count,
    SubmissionsByDay,
    DateUtc,
    TopSurveys,
    SurveyName,
    Status,
    Submissions,
    RatingsSection,
    Rating,
    Percent,
    QuestionTypes,
    QuestionType,
    AnswerCount,
    TextKeywords,
    Keyword,
    KeywordFootnote,
    NoDailyData,
    NoTopSurveys,
    NoRatings,
    NoKeywords,
    EmptyDash,
    FooterBrand,
    Page,
    Of,
    WorkbookTitle,
    SheetSummary,
    SheetBreakdowns,
    SheetDaily,
    SheetTopSurveys,
    SheetRatings,
    SheetQuestionTypes,
    SheetKeywords,
    FilterSection,
    SurveyIdLabel,
    NoteKeywordsNotAi,
    Percentage,
    Section,
    DetailedAnswersSection,
    SubmittedAtCol,
    RespondentCol,
    QuestionCol,
    AnswerCol,
    SheetAnswers,
    NoAnswerDetails,
    PdfAnswersTruncated,
}

public enum OverviewMetricId
{
    SurveysInScope,
    PublishedSurveys,
    SubmittedResponses,
    SurveysWithSubmissions,
    InvitedParticipants,
    ResponsesInProgress,
    AvgSubmissionsPerDay,
    QuestionsInScope,
    ParticipantsCompleted,
    ParticipantsDeclined,
    AvgMinutesToSubmit,
}
