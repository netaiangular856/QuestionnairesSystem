using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestionnairesSystem.Application.Features.Questionnaires.Reports.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;

namespace QuestionnairesSystem.Application.Features.Questionnaires.Reports.Export;

/// <summary>PDF / Excel exports — localized (ar/en), no survey IDs in output; titles only.</summary>
public static class CrossSurveyAnalyticsExportWriter
{
    /// <summary>PDF only — very large tables can exceed viewer limits; Excel always contains full rows.</summary>
    private const int PdfAnswerDetailMaxRows = 8000;

    private enum DistributionKind
    {
        SurveyStatus,
        ResponseStatus,
        Audience,
        Participant,
        Weekday,
        QuestionType,
    }

    private static class Pdf
    {
        public static readonly Color HeaderBar = Color.FromRGB(67, 56, 202);
        public static readonly Color HeaderTextMuted = Color.FromRGB(199, 210, 254);
        public static readonly Color SectionBg = Color.FromRGB(238, 242, 255);
        public static readonly Color SectionBorder = Color.FromRGB(99, 102, 241);
        public static readonly Color TableHead = Color.FromRGB(79, 70, 229);
        public static readonly Color TableHeadText = Color.FromRGB(255, 255, 255);
        public static readonly Color TableBorder = Color.FromRGB(203, 213, 225);
        public static readonly Color RowZebraA = Color.FromRGB(255, 255, 255);
        public static readonly Color RowZebraB = Color.FromRGB(248, 250, 252);
        public static readonly Color PageBg = Color.FromRGB(241, 245, 249);
        public static readonly Color FilterBoxBorder = Color.FromRGB(165, 180, 252);
        public static readonly Color FooterBg = Color.FromRGB(226, 232, 240);
        public static readonly Color TextPrimary = Color.FromRGB(30, 41, 59);
        public static readonly Color TextMuted = Color.FromRGB(100, 116, 139);
        public static readonly Color AccentNote = Color.FromRGB(14, 165, 233);
    }

    private static class Xlsx
    {
        public static readonly XLColor HeaderFill = XLColor.FromArgb(67, 56, 202);
        public static readonly XLColor HeaderFont = XLColor.White;
        public static readonly XLColor TitleFill = XLColor.FromArgb(79, 70, 229);
        public static readonly XLColor TitleFont = XLColor.White;
        public static readonly XLColor ZebraA = XLColor.FromArgb(248, 250, 252);
        public static readonly XLColor ZebraB = XLColor.White;
        public static readonly XLColor LabelFill = XLColor.FromArgb(238, 242, 255);
        public static readonly XLColor Border = XLColor.FromArgb(203, 213, 225);
        public static readonly XLColor TabIndigo = XLColor.FromArgb(99, 102, 241);
        public static readonly XLColor TabTeal = XLColor.FromArgb(20, 184, 166);
        public static readonly XLColor TabAmber = XLColor.FromArgb(245, 158, 11);
    }

    public static byte[] BuildPdf(CrossSurveyAnalyticsDto d, string? langRequest)
    {
        var lang = CrossSurveyAnalyticsReportLocalization.NormalizeLang(langRequest);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

                page.Margin(36);
                page.Size(PageSizes.A4);
                page.PageColor(Pdf.PageBg);

                page.Header().Height(52).Background(Pdf.HeaderBar).PaddingHorizontal(20).AlignMiddle().Row(row =>
                {
                    row.RelativeItem().Text(T(ReportMessageId.ReportTitle)).FontSize(17).SemiBold().FontColor(Colors.White);
                    row.ConstantItem(180).AlignRight()
                        .Text($"{T(ReportMessageId.GeneratedUtc)}\n{DateTime.UtcNow:yyyy-MM-dd HH:mm}")
                        .FontSize(8).LineHeight(1.2f).FontColor(Pdf.HeaderTextMuted).AlignRight();
                });

                page.Content().Padding(16).Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Background(Colors.White).Border(1).BorderColor(Pdf.FilterBoxBorder)
                        .Padding(14).Column(fc =>
                        {
                            fc.Item().Text(T(ReportMessageId.AppliedScope)).SemiBold().FontSize(10).FontColor(Pdf.TableHead);
                            fc.Item().PaddingTop(6).Text(DescribeAppliedFilter(d, lang))
                                .FontSize(9.5f).FontColor(Pdf.TextPrimary).LineHeight(1.35f);
                        });

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.Overview));
                    column.Item().Element(c => OverviewTable(c, d, lang));

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.Distributions));
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(x =>
                            NamedCountBlock(x, lang, T(ReportMessageId.SurveyStatus), d.SurveyStatusDistribution,
                                DistributionKind.SurveyStatus));
                        row.ConstantItem(14);
                        row.RelativeItem().Element(x =>
                            NamedCountBlock(x, lang, T(ReportMessageId.ResponseStatus), d.ResponseStatusDistribution,
                                DistributionKind.ResponseStatus));
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Element(x =>
                            NamedCountBlock(x, lang, T(ReportMessageId.AudienceScope), d.AudienceScopeDistribution,
                                DistributionKind.Audience));
                        row.ConstantItem(14);
                        row.RelativeItem().Element(x =>
                            NamedCountBlock(x, lang, T(ReportMessageId.ParticipantStatus), d.ParticipantStatusDistribution,
                                DistributionKind.Participant));
                    });
                    column.Item().Element(c =>
                        NamedCountBlock(c, lang, T(ReportMessageId.SubmissionsByWeekday), d.SubmissionsByDayOfWeek,
                            DistributionKind.Weekday));

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.SubmissionsByDay));
                    column.Item().Element(c => TimelineTable(c, d.SubmissionsByDay, lang));

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.TopSurveys));
                    column.Item().Element(c => TopSurveysTable(c, d.TopSurveysBySubmissions, lang));

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.RatingsSection));
                    column.Item().Element(c => RatingsTable(c, d.RatingsDistribution, lang));

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.QuestionTypes));
                    column.Item().Element(c =>
                        NamedCountBlock(c, lang, string.Empty, d.QuestionTypeAnswerTotals, DistributionKind.QuestionType));

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.TextKeywords));
                    column.Item().Element(c => KeywordsTable(c, d.TextAnswerKeywords, lang));

                    column.Item().Background(Pdf.SectionBg).BorderLeft(4).BorderColor(Pdf.AccentNote).Padding(10)
                        .Text(T(ReportMessageId.KeywordFootnote))
                        .FontSize(8).Italic().FontColor(Pdf.TextMuted).LineHeight(1.3f);

                    column.Item().Element(c => SectionTitle(c, lang, ReportMessageId.DetailedAnswersSection));
                    column.Item().Element(c => DetailedAnswersTable(c, d, lang));
                });

                page.Footer().Background(Pdf.FooterBg).Padding(10).AlignCenter()
                    .DefaultTextStyle(x => x.FontSize(8).FontColor(Pdf.TextMuted))
                    .Text(t =>
                    {
                        t.Span(T(ReportMessageId.FooterBrand));
                        t.Span("  ·  ");
                        t.Span(T(ReportMessageId.Page) + " ");
                        t.CurrentPageNumber();
                        t.Span(" " + T(ReportMessageId.Of) + " ");
                        t.TotalPages();
                    });
            });
        }).GeneratePdf();
    }

    public static byte[] BuildExcel(CrossSurveyAnalyticsDto d, string? langRequest)
    {
        var lang = CrossSurveyAnalyticsReportLocalization.NormalizeLang(langRequest);
        using var stream = new MemoryStream();
        using (var wb = new XLWorkbook())
        {
            wb.Properties.Title = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.WorkbookTitle);
            wb.Properties.Created = DateTime.UtcNow;

            WriteSummaryWorksheet(wb, d, lang);
            WriteBreakdownsWorksheet(wb, d, lang);
            WriteDailyWorksheet(wb, d, lang);
            WriteTopSurveysWorksheet(wb, d, lang);
            WriteRatingsWorksheet(wb, d, lang);
            WriteQuestionTypesWorksheet(wb, d, lang);
            WriteKeywordsWorksheet(wb, d, lang);
            WriteAnswersWorksheet(wb, d, lang);
            wb.SaveAs(stream);
        }

        return stream.ToArray();
    }

    private static string DescribeAppliedFilter(CrossSurveyAnalyticsDto d, string lang)
    {
        var f = d.AppliedFilter;
        string surveyLine;
        if (!f.SurveyId.HasValue)
        {
            surveyLine = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.AllSurveys);
        }
        else
        {
            surveyLine = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, f.SurveyTitleAr, f.SurveyTitleEn);
            if (string.IsNullOrWhiteSpace(surveyLine))
            {
                surveyLine = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.AllSurveys);
            }
        }

        var dash = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.EmptyDash);
        var from = f.FromUtc.HasValue ? f.FromUtc.Value.ToString("u") : dash;
        var to = f.ToUtc.HasValue ? f.ToUtc.Value.ToString("u") : dash;
        var scopeLabel = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.SurveyScope);
        var fromLabel = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.FromUtc);
        var toLabel = CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.ToUtc);
        return $"{scopeLabel}: {surveyLine}\n{fromLabel}: {from}\n{toLabel}: {to}";
    }

    private static void SectionTitle(IContainer container, string lang, ReportMessageId id)
    {
        var text = CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        container.Background(Pdf.SectionBg).BorderLeft(5).BorderColor(Pdf.SectionBorder)
            .PaddingVertical(8).PaddingHorizontal(12)
            .Text(text).SemiBold().FontSize(11.5f).FontColor(Pdf.TableHead);
    }

    private static IContainer TableWrap(IContainer c) =>
        c.Background(Colors.White).Border(1).BorderColor(Pdf.TableBorder).Padding(1);

    private static void OverviewTable(IContainer container, CrossSurveyAnalyticsDto d, string lang)
    {
        var o = d.Overview;
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        string Om(OverviewMetricId id) => CrossSurveyAnalyticsReportLocalization.OverviewMetric(lang, id);

        container.Element(TableWrap).Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.RelativeColumn(2);
                cols.RelativeColumn();
            });
            table.Header(h =>
            {
                h.Cell().Element(Th).Text(T(ReportMessageId.Metric));
                h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Value));
            });
            void RowKpi(int i, OverviewMetricId metricId, string value)
            {
                table.Cell().Element(c => Td(c, i)).Text(Om(metricId));
                table.Cell().Element(c => Td(c, i)).AlignRight().Text(value);
            }

            RowKpi(0, OverviewMetricId.SurveysInScope, o.SurveysInScope.ToString(CultureInfo.InvariantCulture));
            RowKpi(1, OverviewMetricId.PublishedSurveys, o.PublishedSurveys.ToString(CultureInfo.InvariantCulture));
            RowKpi(2, OverviewMetricId.SubmittedResponses, o.SubmittedResponsesInPeriod.ToString(CultureInfo.InvariantCulture));
            RowKpi(3, OverviewMetricId.SurveysWithSubmissions, o.SurveysWithSubmissionsInPeriod.ToString(CultureInfo.InvariantCulture));
            RowKpi(4, OverviewMetricId.InvitedParticipants, o.InvitedParticipantsInScope.ToString(CultureInfo.InvariantCulture));
            RowKpi(5, OverviewMetricId.ResponsesInProgress, o.InProgressResponsesOpen.ToString(CultureInfo.InvariantCulture));
            RowKpi(6, OverviewMetricId.AvgSubmissionsPerDay, o.SubmissionsPerDayInPeriod.ToString("0.##", CultureInfo.InvariantCulture));
            RowKpi(7, OverviewMetricId.QuestionsInScope, o.TotalQuestionsInScope.ToString(CultureInfo.InvariantCulture));
            RowKpi(8, OverviewMetricId.ParticipantsCompleted, o.CompletedParticipantsInScope.ToString(CultureInfo.InvariantCulture));
            RowKpi(9, OverviewMetricId.ParticipantsDeclined, o.DeclinedParticipantsInScope.ToString(CultureInfo.InvariantCulture));
            RowKpi(10, OverviewMetricId.AvgMinutesToSubmit, o.AverageMinutesToSubmitInPeriod.ToString("0.#", CultureInfo.InvariantCulture));
        });
    }

    private static string TranslateKey(string lang, DistributionKind kind, string key) =>
        kind switch
        {
            DistributionKind.SurveyStatus => CrossSurveyAnalyticsReportLocalization.TranslateSurveyStatus(lang, key),
            DistributionKind.ResponseStatus => CrossSurveyAnalyticsReportLocalization.TranslateResponseStatus(lang, key),
            DistributionKind.Audience => CrossSurveyAnalyticsReportLocalization.TranslateAudienceScope(lang, key),
            DistributionKind.Participant => CrossSurveyAnalyticsReportLocalization.TranslateParticipantStatus(lang, key),
            DistributionKind.Weekday => CrossSurveyAnalyticsReportLocalization.TranslateDayOfWeek(lang, key),
            DistributionKind.QuestionType => CrossSurveyAnalyticsReportLocalization.TranslateQuestionType(lang, key),
            _ => key,
        };

    private static void NamedCountBlock(
        IContainer container,
        string lang,
        string title,
        IReadOnlyList<NamedCountDto> rows,
        DistributionKind kind)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        container.Column(col =>
        {
            col.Spacing(6);
            if (!string.IsNullOrEmpty(title))
            {
                col.Item().Text(title).SemiBold().FontSize(10).FontColor(Pdf.TextPrimary);
            }

            if (rows.Count == 0)
            {
                col.Item().Text(T(ReportMessageId.EmptyDash)).Italic().FontColor(Pdf.TextMuted).FontSize(9);
                return;
            }

            col.Item().Element(TableWrap).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2);
                    c.RelativeColumn();
                });
                table.Header(h =>
                {
                    h.Cell().Element(Th).Text(T(ReportMessageId.Key));
                    h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Count));
                });
                var i = 0;
                foreach (var r in rows)
                {
                    table.Cell().Element(c => Td(c, i)).Text(TranslateKey(lang, kind, r.Key));
                    table.Cell().Element(c => Td(c, i)).AlignRight().Text(r.Count.ToString(CultureInfo.InvariantCulture));
                    i++;
                }
            });
        });
    }

    private static void TimelineTable(IContainer container, IReadOnlyList<TimelinePointDto> points, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        if (points.Count == 0)
        {
            container.Text(T(ReportMessageId.NoDailyData)).Italic().FontColor(Pdf.TextMuted).FontSize(9);
            return;
        }

        container.Element(TableWrap).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn();
                c.ConstantColumn(88);
            });
            table.Header(h =>
            {
                h.Cell().Element(Th).Text(T(ReportMessageId.DateUtc));
                h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Count));
            });
            var i = 0;
            foreach (var p in points)
            {
                table.Cell().Element(c => Td(c, i)).Text(p.Date);
                table.Cell().Element(c => Td(c, i)).AlignRight().Text(p.Count.ToString(CultureInfo.InvariantCulture));
                i++;
            }
        });
    }

    private static void TopSurveysTable(IContainer container, IReadOnlyList<TopSurveyRowDto> rows, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        if (rows.Count == 0)
        {
            container.Text(T(ReportMessageId.NoTopSurveys)).Italic().FontColor(Pdf.TextMuted).FontSize(9);
            return;
        }

        container.Element(TableWrap).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(3);
                c.RelativeColumn();
                c.ConstantColumn(72);
            });
            table.Header(h =>
            {
                h.Cell().Element(Th).Text(T(ReportMessageId.SurveyName));
                h.Cell().Element(Th).Text(T(ReportMessageId.Status));
                h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Submissions));
            });
            var i = 0;
            foreach (var r in rows)
            {
                var name = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, r.TitleAr, r.TitleEn);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = T(ReportMessageId.EmptyDash);
                }

                var statusLabel = CrossSurveyAnalyticsReportLocalization.TranslateSurveyStatus(lang, r.Status);
                table.Cell().Element(c => Td(c, i)).Text(name);
                table.Cell().Element(c => Td(c, i)).Text(statusLabel);
                table.Cell().Element(c => Td(c, i)).AlignRight().Text(r.SubmissionsInPeriod.ToString(CultureInfo.InvariantCulture));
                i++;
            }
        });
    }

    private static void RatingsTable(IContainer container, IReadOnlyList<RatingAnalyticsDto> rows, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        if (rows.Count == 0)
        {
            container.Text(T(ReportMessageId.NoRatings)).Italic().FontColor(Pdf.TextMuted).FontSize(9);
            return;
        }

        container.Element(TableWrap).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(72);
                c.ConstantColumn(72);
                c.ConstantColumn(80);
            });
            table.Header(h =>
            {
                h.Cell().Element(Th).Text(T(ReportMessageId.Rating));
                h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Count));
                h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Percent));
            });
            var i = 0;
            foreach (var r in rows)
            {
                table.Cell().Element(c => Td(c, i)).Text(r.Rating.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(c => Td(c, i)).AlignRight().Text(r.Count.ToString(CultureInfo.InvariantCulture));
                table.Cell().Element(c => Td(c, i)).AlignRight().Text(r.Percentage.ToString("0.#", CultureInfo.InvariantCulture));
                i++;
            }
        });
    }

    private static void KeywordsTable(IContainer container, IReadOnlyList<KeywordCountDto> rows, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        if (rows.Count == 0)
        {
            container.Text(T(ReportMessageId.NoKeywords)).Italic().FontColor(Pdf.TextMuted).FontSize(9);
            return;
        }

        container.Element(TableWrap).Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2);
                c.ConstantColumn(80);
            });
            table.Header(h =>
            {
                h.Cell().Element(Th).Text(T(ReportMessageId.Keyword));
                h.Cell().Element(Th).AlignRight().Text(T(ReportMessageId.Count));
            });
            var i = 0;
            foreach (var r in rows)
            {
                table.Cell().Element(c => Td(c, i)).Text(r.Keyword);
                table.Cell().Element(c => Td(c, i)).AlignRight().Text(r.Count.ToString(CultureInfo.InvariantCulture));
                i++;
            }
        });
    }

    private static void DetailedAnswersTable(IContainer container, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        var rows = d.AnswerDetails;
        if (rows.Count == 0)
        {
            container.Text(T(ReportMessageId.NoAnswerDetails)).Italic().FontColor(Pdf.TextMuted).FontSize(9);
            return;
        }

        var truncated = rows.Count > PdfAnswerDetailMaxRows;
        IEnumerable<CrossSurveyAnswerDetailRowDto> pdfRowsEnum = truncated ? rows.Take(PdfAnswerDetailMaxRows) : rows;

        container.Column(col =>
        {
            col.Item().Element(TableWrap).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.3f);
                    c.ConstantColumn(78);
                    c.RelativeColumn(0.9f);
                    c.RelativeColumn(1.6f);
                    c.ConstantColumn(62);
                    c.RelativeColumn(2.2f);
                });
                table.Header(h =>
                {
                    h.Cell().Element(Th).Text(T(ReportMessageId.SurveyName));
                    h.Cell().Element(Th).Text(T(ReportMessageId.SubmittedAtCol));
                    h.Cell().Element(Th).Text(T(ReportMessageId.RespondentCol));
                    h.Cell().Element(Th).Text(T(ReportMessageId.QuestionCol));
                    h.Cell().Element(Th).Text(T(ReportMessageId.QuestionType));
                    h.Cell().Element(Th).Text(T(ReportMessageId.AnswerCol));
                });
                var i = 0;
                foreach (var r in pdfRowsEnum)
                {
                    var surveyName = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, r.SurveyTitleAr, r.SurveyTitleEn);
                    if (string.IsNullOrWhiteSpace(surveyName))
                    {
                        surveyName = T(ReportMessageId.EmptyDash);
                    }

                    var qTitle = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, r.QuestionTitleAr, r.QuestionTitleEn);
                    if (string.IsNullOrWhiteSpace(qTitle))
                    {
                        qTitle = T(ReportMessageId.EmptyDash);
                    }

                    var respondent = string.IsNullOrWhiteSpace(r.RespondentDisplayName)
                        ? T(ReportMessageId.EmptyDash)
                        : r.RespondentDisplayName!;
                    var submitted = r.SubmittedAtUtc.HasValue
                        ? r.SubmittedAtUtc.Value.ToString("u", CultureInfo.InvariantCulture)
                        : T(ReportMessageId.EmptyDash);
                    var qType = CrossSurveyAnalyticsReportLocalization.TranslateQuestionType(lang, r.QuestionTypeKey);
                    var answer = CrossSurveyAnalyticsReportLocalization.IsArabic(lang) ? r.AnswerTextAr : r.AnswerTextEn;

                    table.Cell().Element(c => TdSmall(c, i)).Text(surveyName);
                    table.Cell().Element(c => TdSmall(c, i)).Text(submitted);
                    table.Cell().Element(c => TdSmall(c, i)).Text(respondent);
                    table.Cell().Element(c => TdSmall(c, i)).Text(qTitle);
                    table.Cell().Element(c => TdSmall(c, i)).Text(qType);
                    table.Cell().Element(c => TdSmall(c, i)).Text(string.IsNullOrWhiteSpace(answer) ? T(ReportMessageId.EmptyDash) : answer);
                    i++;
                }
            });

            if (truncated)
            {
                col.Item().PaddingTop(8).Text(string.Format(T(ReportMessageId.PdfAnswersTruncated), PdfAnswerDetailMaxRows))
                    .FontSize(8).Italic().FontColor(Pdf.TextMuted).LineHeight(1.25f);
            }
        });
    }

    private static IContainer TdSmall(IContainer c, int rowIndex)
    {
        var bg = rowIndex % 2 == 0 ? Pdf.RowZebraA : Pdf.RowZebraB;
        return c.Background(bg).BorderBottom(0.5f).BorderColor(Pdf.TableBorder)
            .DefaultTextStyle(x => x.FontSize(7.5f).FontColor(Pdf.TextPrimary))
            .PaddingVertical(4).PaddingHorizontal(6);
    }

    private static IContainer Th(IContainer c) =>
        c.Background(Pdf.TableHead).BorderBottom(2).BorderColor(Pdf.TableHead)
            .DefaultTextStyle(x => x.SemiBold().FontColor(Pdf.TableHeadText).FontSize(9))
            .PaddingVertical(7).PaddingHorizontal(8);

    private static IContainer Td(IContainer c, int rowIndex)
    {
        var bg = rowIndex % 2 == 0 ? Pdf.RowZebraA : Pdf.RowZebraB;
        return c.Background(bg).BorderBottom(0.5f).BorderColor(Pdf.TableBorder)
            .DefaultTextStyle(x => x.FontSize(9).FontColor(Pdf.TextPrimary))
            .PaddingVertical(5).PaddingHorizontal(8);
    }

    private static void WriteSummaryWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);
        string Om(OverviewMetricId id) => CrossSurveyAnalyticsReportLocalization.OverviewMetric(lang, id);

        var ws = wb.Worksheets.Add(CrossSurveyAnalyticsReportLocalization.Message(lang, ReportMessageId.SheetSummary));
        ws.TabColor = Xlsx.TabIndigo;

        ws.Range(1, 1, 1, 4).Merge();
        ws.Cell(1, 1).Value = T(ReportMessageId.ReportTitle) + " — " + T(ReportMessageId.SheetSummary);
        StyleTitleRow(ws, 1, 4);

        ws.Cell(3, 1).Value = T(ReportMessageId.FilterSection);
        ws.Range(3, 1, 3, 4).Merge();
        ws.Cell(3, 1).Style.Font.Bold = true;
        ws.Cell(3, 1).Style.Font.FontColor = Xlsx.TitleFill;
        ws.Cell(3, 1).Style.Fill.BackgroundColor = Xlsx.LabelFill;

        var f = d.AppliedFilter;
        string surveyVal;
        if (!f.SurveyId.HasValue)
        {
            surveyVal = T(ReportMessageId.AllSurveys);
        }
        else
        {
            surveyVal = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, f.SurveyTitleAr, f.SurveyTitleEn);
            if (string.IsNullOrWhiteSpace(surveyVal))
            {
                surveyVal = T(ReportMessageId.AllSurveys);
            }
        }

        var dash = T(ReportMessageId.EmptyDash);
        ws.Cell(4, 1).Value = T(ReportMessageId.SurveyScope);
        ws.Cell(4, 2).Value = surveyVal;
        ws.Cell(5, 1).Value = T(ReportMessageId.FromUtc);
        ws.Cell(5, 2).Value = f.FromUtc?.ToString("u") ?? dash;
        ws.Cell(6, 1).Value = T(ReportMessageId.ToUtc);
        ws.Cell(6, 2).Value = f.ToUtc?.ToString("u") ?? dash;
        ApplyLabelColumn(ws.Range(4, 1, 6, 1));
        OutlineRange(ws.Range(4, 1, 6, 4));

        var o = d.Overview;
        var r = 8;
        ws.Cell(r, 1).Value = T(ReportMessageId.Metric);
        ws.Cell(r, 2).Value = T(ReportMessageId.Value);
        StyleHeaderRow(ws.Range(r, 1, r, 2));
        r++;
        void Kpi(OverviewMetricId metricId, string b, int idx)
        {
            ws.Cell(r, 1).Value = Om(metricId);
            ws.Cell(r, 2).Value = b;
            ApplyZebraRow(ws.Range(r, 1, r, 2), idx);
            r++;
        }

        Kpi(OverviewMetricId.SurveysInScope, o.SurveysInScope.ToString(CultureInfo.InvariantCulture), 0);
        Kpi(OverviewMetricId.PublishedSurveys, o.PublishedSurveys.ToString(CultureInfo.InvariantCulture), 1);
        Kpi(OverviewMetricId.SubmittedResponses, o.SubmittedResponsesInPeriod.ToString(CultureInfo.InvariantCulture), 2);
        Kpi(OverviewMetricId.SurveysWithSubmissions, o.SurveysWithSubmissionsInPeriod.ToString(CultureInfo.InvariantCulture), 3);
        Kpi(OverviewMetricId.InvitedParticipants, o.InvitedParticipantsInScope.ToString(CultureInfo.InvariantCulture), 4);
        Kpi(OverviewMetricId.ResponsesInProgress, o.InProgressResponsesOpen.ToString(CultureInfo.InvariantCulture), 5);
        Kpi(OverviewMetricId.AvgSubmissionsPerDay, o.SubmissionsPerDayInPeriod.ToString("0.##", CultureInfo.InvariantCulture), 6);
        Kpi(OverviewMetricId.QuestionsInScope, o.TotalQuestionsInScope.ToString(CultureInfo.InvariantCulture), 7);
        Kpi(OverviewMetricId.ParticipantsCompleted, o.CompletedParticipantsInScope.ToString(CultureInfo.InvariantCulture), 8);
        Kpi(OverviewMetricId.ParticipantsDeclined, o.DeclinedParticipantsInScope.ToString(CultureInfo.InvariantCulture), 9);
        Kpi(OverviewMetricId.AvgMinutesToSubmit, o.AverageMinutesToSubmitInPeriod.ToString("0.#", CultureInfo.InvariantCulture), 10);

        OutlineRange(ws.Range(8, 1, r - 1, 2));
        ws.SheetView.FreezeRows(8);
        ws.Columns(1, 4).AdjustToContents();
    }

    private static void WriteBreakdownsWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetBreakdowns));
        ws.TabColor = Xlsx.TabTeal;

        ws.Cell(1, 1).Value = T(ReportMessageId.Section);
        ws.Cell(1, 2).Value = T(ReportMessageId.Key);
        ws.Cell(1, 3).Value = T(ReportMessageId.Count);
        StyleHeaderRow(ws.Range(1, 1, 1, 3));

        var row = 2;
        void Dump(string sectionLabel, IReadOnlyList<NamedCountDto> items, DistributionKind kind)
        {
            foreach (var x in items)
            {
                ws.Cell(row, 1).Value = sectionLabel;
                ws.Cell(row, 2).Value = TranslateKey(lang, kind, x.Key);
                ws.Cell(row, 3).Value = x.Count;
                ApplyZebraRow(ws.Range(row, 1, row, 3), row - 2);
                row++;
            }
        }

        Dump(T(ReportMessageId.SurveyStatus), d.SurveyStatusDistribution, DistributionKind.SurveyStatus);
        Dump(T(ReportMessageId.ResponseStatus), d.ResponseStatusDistribution, DistributionKind.ResponseStatus);
        Dump(T(ReportMessageId.AudienceScope), d.AudienceScopeDistribution, DistributionKind.Audience);
        Dump(T(ReportMessageId.ParticipantStatus), d.ParticipantStatusDistribution, DistributionKind.Participant);
        Dump(T(ReportMessageId.SubmissionsByWeekday), d.SubmissionsByDayOfWeek, DistributionKind.Weekday);

        if (row > 2)
        {
            OutlineRange(ws.Range(1, 1, row - 1, 3));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 3).AdjustToContents();
    }

    private static void WriteDailyWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetDaily));
        ws.TabColor = Xlsx.TabAmber;

        ws.Cell(1, 1).Value = T(ReportMessageId.DateUtc);
        ws.Cell(1, 2).Value = T(ReportMessageId.Count);
        StyleHeaderRow(ws.Range(1, 1, 1, 2));

        var r = 2;
        var i = 0;
        foreach (var p in d.SubmissionsByDay)
        {
            ws.Cell(r, 1).Value = p.Date;
            ws.Cell(r, 2).Value = p.Count;
            ApplyZebraRow(ws.Range(r, 1, r, 2), i);
            r++;
            i++;
        }

        if (r > 2)
        {
            OutlineRange(ws.Range(1, 1, r - 1, 2));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 2).AdjustToContents();
    }

    private static void WriteAnswersWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetAnswers));
        ws.TabColor = Xlsx.TabTeal;

        ws.Cell(1, 1).Value = T(ReportMessageId.SurveyName);
        ws.Cell(1, 2).Value = T(ReportMessageId.SubmittedAtCol);
        ws.Cell(1, 3).Value = T(ReportMessageId.RespondentCol);
        ws.Cell(1, 4).Value = T(ReportMessageId.QuestionCol);
        ws.Cell(1, 5).Value = T(ReportMessageId.QuestionType);
        ws.Cell(1, 6).Value = T(ReportMessageId.AnswerCol);
        StyleHeaderRow(ws.Range(1, 1, 1, 6));

        var r = 2;
        var i = 0;
        foreach (var x in d.AnswerDetails)
        {
            var surveyName = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, x.SurveyTitleAr, x.SurveyTitleEn);
            if (string.IsNullOrWhiteSpace(surveyName))
            {
                surveyName = T(ReportMessageId.EmptyDash);
            }

            var qTitle = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, x.QuestionTitleAr, x.QuestionTitleEn);
            if (string.IsNullOrWhiteSpace(qTitle))
            {
                qTitle = T(ReportMessageId.EmptyDash);
            }

            var respondent = string.IsNullOrWhiteSpace(x.RespondentDisplayName)
                ? T(ReportMessageId.EmptyDash)
                : x.RespondentDisplayName!;
            var submitted = x.SubmittedAtUtc?.ToString("u", CultureInfo.InvariantCulture) ?? T(ReportMessageId.EmptyDash);
            var qType = CrossSurveyAnalyticsReportLocalization.TranslateQuestionType(lang, x.QuestionTypeKey);
            var answer = CrossSurveyAnalyticsReportLocalization.IsArabic(lang) ? x.AnswerTextAr : x.AnswerTextEn;
            if (string.IsNullOrWhiteSpace(answer))
            {
                answer = T(ReportMessageId.EmptyDash);
            }

            ws.Cell(r, 1).Value = surveyName;
            ws.Cell(r, 2).Value = submitted;
            ws.Cell(r, 3).Value = respondent;
            ws.Cell(r, 4).Value = qTitle;
            ws.Cell(r, 5).Value = qType;
            ws.Cell(r, 6).Value = answer;
            ApplyZebraRow(ws.Range(r, 1, r, 6), i);
            r++;
            i++;
        }

        if (r > 2)
        {
            OutlineRange(ws.Range(1, 1, r - 1, 6));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 6).AdjustToContents();
    }

    private static void WriteTopSurveysWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetTopSurveys));
        ws.TabColor = Xlsx.TabIndigo;

        ws.Cell(1, 1).Value = T(ReportMessageId.SurveyName);
        ws.Cell(1, 2).Value = T(ReportMessageId.Status);
        ws.Cell(1, 3).Value = T(ReportMessageId.Submissions);
        StyleHeaderRow(ws.Range(1, 1, 1, 3));

        var r = 2;
        var i = 0;
        foreach (var x in d.TopSurveysBySubmissions)
        {
            var name = CrossSurveyAnalyticsReportLocalization.PickSurveyTitle(lang, x.TitleAr, x.TitleEn);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = T(ReportMessageId.EmptyDash);
            }

            ws.Cell(r, 1).Value = name;
            ws.Cell(r, 2).Value = CrossSurveyAnalyticsReportLocalization.TranslateSurveyStatus(lang, x.Status);
            ws.Cell(r, 3).Value = x.SubmissionsInPeriod;
            ApplyZebraRow(ws.Range(r, 1, r, 3), i);
            r++;
            i++;
        }

        if (r > 2)
        {
            OutlineRange(ws.Range(1, 1, r - 1, 3));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 3).AdjustToContents();
    }

    private static void WriteRatingsWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetRatings));
        ws.TabColor = Xlsx.TabTeal;

        ws.Cell(1, 1).Value = T(ReportMessageId.Rating);
        ws.Cell(1, 2).Value = T(ReportMessageId.Count);
        ws.Cell(1, 3).Value = T(ReportMessageId.Percentage);
        StyleHeaderRow(ws.Range(1, 1, 1, 3));

        var r = 2;
        var i = 0;
        foreach (var x in d.RatingsDistribution)
        {
            ws.Cell(r, 1).Value = x.Rating;
            ws.Cell(r, 2).Value = x.Count;
            ws.Cell(r, 3).Value = x.Percentage;
            ws.Cell(r, 3).Style.NumberFormat.Format = "0.0";
            ApplyZebraRow(ws.Range(r, 1, r, 3), i);
            r++;
            i++;
        }

        if (r > 2)
        {
            OutlineRange(ws.Range(1, 1, r - 1, 3));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 3).AdjustToContents();
    }

    private static void WriteQuestionTypesWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetQuestionTypes));
        ws.TabColor = Xlsx.TabAmber;

        ws.Cell(1, 1).Value = T(ReportMessageId.QuestionType);
        ws.Cell(1, 2).Value = T(ReportMessageId.AnswerCount);
        StyleHeaderRow(ws.Range(1, 1, 1, 2));

        var r = 2;
        var i = 0;
        foreach (var x in d.QuestionTypeAnswerTotals)
        {
            ws.Cell(r, 1).Value = CrossSurveyAnalyticsReportLocalization.TranslateQuestionType(lang, x.Key);
            ws.Cell(r, 2).Value = x.Count;
            ApplyZebraRow(ws.Range(r, 1, r, 2), i);
            r++;
            i++;
        }

        if (r > 2)
        {
            OutlineRange(ws.Range(1, 1, r - 1, 2));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 2).AdjustToContents();
    }

    private static void WriteKeywordsWorksheet(XLWorkbook wb, CrossSurveyAnalyticsDto d, string lang)
    {
        string T(ReportMessageId id) => CrossSurveyAnalyticsReportLocalization.Message(lang, id);

        var ws = wb.Worksheets.Add(T(ReportMessageId.SheetKeywords));
        ws.TabColor = Xlsx.TabIndigo;

        ws.Cell(1, 1).Value = T(ReportMessageId.Keyword);
        ws.Cell(1, 2).Value = T(ReportMessageId.Count);
        StyleHeaderRow(ws.Range(1, 1, 1, 2));

        ws.Cell(2, 1).Value = T(ReportMessageId.NoteKeywordsNotAi);
        ws.Range(2, 1, 2, 2).Merge();
        ws.Cell(2, 1).Style.Fill.BackgroundColor = Xlsx.LabelFill;
        ws.Cell(2, 1).Style.Font.Italic = true;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(71, 85, 105);

        var r = 3;
        var i = 0;
        foreach (var x in d.TextAnswerKeywords)
        {
            ws.Cell(r, 1).Value = x.Keyword;
            ws.Cell(r, 2).Value = x.Count;
            ApplyZebraRow(ws.Range(r, 1, r, 2), i);
            r++;
            i++;
        }

        if (r > 3)
        {
            OutlineRange(ws.Range(1, 1, r - 1, 2));
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, 2).AdjustToContents();
    }

    private static void StyleTitleRow(IXLWorksheet ws, int firstCol, int lastCol)
    {
        var range = ws.Range(1, firstCol, 1, lastCol);
        range.Style.Fill.BackgroundColor = Xlsx.TitleFill;
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = Xlsx.TitleFont;
        range.Style.Font.FontSize = 16;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 36;
    }

    private static void StyleHeaderRow(IXLRange range)
    {
        range.Style.Fill.BackgroundColor = Xlsx.HeaderFill;
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = Xlsx.HeaderFont;
        range.Style.Font.FontSize = 11;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thick;
        range.Style.Border.BottomBorderColor = Xlsx.HeaderFill;
    }

    private static void ApplyZebraRow(IXLRange range, int zebraIndex)
    {
        var fill = zebraIndex % 2 == 0 ? Xlsx.ZebraA : Xlsx.ZebraB;
        range.Style.Fill.BackgroundColor = fill;
        range.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        range.Style.Border.BottomBorderColor = Xlsx.Border;
    }

    private static void ApplyLabelColumn(IXLRange singleColumn)
    {
        singleColumn.Style.Fill.BackgroundColor = Xlsx.LabelFill;
        singleColumn.Style.Font.Bold = true;
        singleColumn.Style.Font.FontColor = XLColor.FromArgb(67, 56, 202);
    }

    private static void OutlineRange(IXLRange range)
    {
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        range.Style.Border.OutsideBorderColor = Xlsx.Border;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorderColor = Xlsx.Border;
    }
}
