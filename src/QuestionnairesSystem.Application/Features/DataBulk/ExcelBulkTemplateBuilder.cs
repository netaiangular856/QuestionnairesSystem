using ClosedXML.Excel;

namespace QuestionnairesSystem.Application.Features.DataBulk;

internal static partial class ExcelBulkTemplateBuilder
{
    public const string SheetInstructions = "_Instructions";
    public const string SheetDepartments = "Departments";
    public const string SheetEmployees = "Employees";
    public const string SheetPartners = "Partners";
    public const string SheetUsers = "Users";
    public const string SheetSurveys = "Surveys";
    public const string SheetSurveyQuestions = "SurveyQuestions";
    public const string SheetSurveyResponses = "SurveyResponses";
    public const string SheetSurveyTemplates = "SurveyTemplates";
    public const string SheetSurveyTemplateQuestions = "SurveyTemplateQuestions";
    public const string SheetRecommendations = "Recommendations";
    public const string SheetActionPlans = "ActionPlans";
    public const string SheetInitiatives = "Initiatives";

    public static byte[] Build(ExcelTemplateScope scope, bool includeSamples)
    {
        using var wb = new XLWorkbook();
        var fullGuide = scope == ExcelTemplateScope.All;
        if (fullGuide)
        {
            AddFullInstructionsSheet(wb);
        }
        else
        {
            AddScopeHintSheet(wb, scope);
        }

        switch (scope)
        {
            case ExcelTemplateScope.All:
                AddDepartmentsSheet(wb, includeSamples);
                AddEmployeesSheet(wb, includeSamples);
                AddPartnersSheet(wb, includeSamples);
                AddUsersSheet(wb, includeSamples);
                AddSurveyTemplatesSheet(wb, includeSamples);
                AddSurveyTemplateQuestionsSheet(wb, includeSamples);
                AddSurveysSheet(wb, includeSamples);
                AddSurveyQuestionsSheet(wb, includeSamples);
                AddSurveyResponsesSheet(wb, includeSamples);
                AddRecommendationsSheet(wb, includeSamples);
                AddActionPlansSheet(wb, includeSamples);
                AddInitiativesSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Departments:
                AddDepartmentsSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Employees:
                AddEmployeesSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Partners:
                AddPartnersSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Users:
                AddUsersSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Templates:
                AddSurveyTemplatesSheet(wb, includeSamples);
                AddSurveyTemplateQuestionsSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Surveys:
                AddSurveysSheet(wb, includeSamples);
                AddSurveyQuestionsSheet(wb, includeSamples);
                AddSurveyResponsesSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Recommendations:
                AddRecommendationsSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.ActionPlans:
                AddActionPlansSheet(wb, includeSamples);
                break;
            case ExcelTemplateScope.Initiatives:
                // Initiatives need an ActionPlan parent; ship both sheets so the file is self-contained.
                AddActionPlansSheet(wb, includeSamples);
                AddInitiativesSheet(wb, includeSamples);
                break;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void AddScopeHintSheet(XLWorkbook wb, ExcelTemplateScope scope)
    {
        var title = scope switch
        {
            ExcelTemplateScope.Departments => "هذا الملف: أعمدة الأقسام فقط. املأ من الصف 2. للاستيراد الكامل استخدم نطاق «الكل» من الواجهة.",
            ExcelTemplateScope.Employees => "هذا الملف: أعمدة الموظفين فقط. DepartmentCode يجب أن يطابق كود قسم موجود مسبقاً.",
            ExcelTemplateScope.Partners => "هذا الملف: أعمدة الشركاء فقط. Type: 1–4 أو 99.",
            ExcelTemplateScope.Users => "هذا الملف: المستخدمون فقط. RoleNames بالإنجليزية (مثل Employee, Admin).",
            ExcelTemplateScope.Templates => "هذا الملف: SurveyTemplates + SurveyTemplateQuestions. ربط TemplateKey بين الورقتين.",
            ExcelTemplateScope.Surveys => "هذا الملف: Surveys + SurveyQuestions + SurveyResponses. ربط SurveyKey بين الأوراق الثلاث.",
            ExcelTemplateScope.Recommendations =>
                "هذا الملف: التوصيات فقط. SurveyKey اختياري ويطابق Code استبيان موجود. AssignedToUserName اختياري ويطابق UserName مستخدم موجود.",
            ExcelTemplateScope.ActionPlans =>
                "هذا الملف: خطط العمل فقط. ActionPlanKey مفتاح داخلي للورقة (يستخدمه ورقة Initiatives لاحقاً). SurveyKey/OwnerUserName اختياريان ويطابقان عناصر موجودة.",
            ExcelTemplateScope.Initiatives =>
                "هذا الملف: ActionPlans + Initiatives. ActionPlanKey مطلوب في الورقتين ليربط كل مبادرة بخطتها داخل نفس الملف.",
            _ => "قالب جزئي.",
        };

        var ws = wb.Worksheets.Add(SheetInstructions);
        ws.Cell(1, 1).Value = title + "\r\n\r\nPartial template — Questionnaires OS";
        ws.Cell(1, 1).Style.Alignment.WrapText = true;
        ws.Range(1, 1, 8, 6).Merge();
        ws.Column(1).Width = 90;
        ws.Row(1).Height = 120;
    }

    private static void AddFullInstructionsSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add(SheetInstructions);
        ws.Cell(1, 1).Value =
            "قالب استيراد البيانات — Questionnaires OS\r\n" +
            "Bulk data template\r\n\r\n" +
            "ترتيب التعبئة الموصى به:\r\n" +
            "1) Departments — الأقسام (يمكن ترك ParentDepartmentCode فارغاً للجذر، أو كود القسم الأب).\r\n" +
            "2) Employees — الموظفون (DepartmentCode يطابق عمود Code من Departments).\r\n" +
            "3) Partners — الشركاء (Type: 1 Dealer, 2 Partner, 3 Vendor, 4 Customer, 99 Other).\r\n" +
            "4) Users — المستخدمون (RoleNames: أسماء الأدوار بالإنجليزية مفصولة بفاصلة، مثل: Employee أو Admin,Employee).\r\n" +
            "5) SurveyTemplates ثم SurveyTemplateQuestions — قوالب استبيان جاهزة (TemplateKey يربط بين الورقتين؛ يُستورد قبل تعريف الاستبيانات).\r\n" +
            "6) Surveys ثم SurveyQuestions — تعريف الاستبيان والأسئلة (SurveyKey رابط بين الأوراق).\r\n" +
            "   في العيّنة: استبيان IMP-SVY-01 يضم تسعة أسئلة (أنواع 1–9 كاملة)؛ بقية الصفوف في SurveyQuestions توزّع أسئلة على استبيانات أخرى حتى 20 صفاً.\r\n" +
            "7) SurveyResponses — إجابات (SurveyKey، UserName، QuestionDisplayOrder، AnswerValue). في العيّنة: تسعة ردود لـ IMP-SVY-01 (نفس المستخدم) تغطي كل الأسئلة، ثم ردود لاستبيانات أخرى حتى 20 صفاً.\r\n" +
            "8) Recommendations — التوصيات (SurveyKey و AssignedToUserName اختياريان).\r\n" +
            "9) ActionPlans — خطط العمل (ActionPlanKey مفتاح داخلي للملف، SurveyKey/OwnerUserName اختياريان).\r\n" +
            "10) Initiatives — المبادرات (ActionPlanKey يطابق ورقة ActionPlans؛ OwnerUserName اختياري).\r\n\r\n" +
            "AudienceScope (Surveys): 1 Everyone, 2 Guest, 3 SpecificUsers, 4 AllOrganizationMembers.\r\n" +
            "QuestionType (SurveyQuestions و SurveyTemplateQuestions): 1 ShortText, 2 LongText, 3 SingleChoice, 4 MultipleChoice, 5 Rating, 6 Scale, 7 YesNo, 8 Date, 9 Number.\r\n" +
            "OptionsJson للاختيارات: [{\"value\":\"a\",\"labelAr\":\"خيار\",\"labelEn\":\"Option\"}] — وللمقياس مثلاً {\"min\":1,\"max\":5}.\r\n" +
            "عمود SurveyKey في SurveyResponses و Recommendations و ActionPlans يجب أن يطابق قيمة Code المحفوظة للاستبيان (= PublicCode إن وُجد، وإلا SurveyKey في ورقة Surveys).\r\n" +
            "أعمدة التواريخ (DueDateUtc / StartDateUtc / EndDateUtc / TargetDateUtc) بصيغة ISO مثل 2026-05-31.\r\n" +
            "أمثلة الصفوف (عند اختيار «قالب مع أمثلة»): 20 صفاً لكل ورقة بيانات تقريباً، بمفردات قريبة من سياق دولة الإمارات (متعامل، هوية رقمية، رؤية 2071، إلخ). تستخدم بادئة IMP- لتقليل التصادم؛ غيّرها قبل الإنتاج.\r\n" +
            "القالب الفارغ: صف العناوين فقط. القالب مع أمثلة: صفوف توضيحية يمكن حذفها أو استبدالها.\r\n";
        ws.Cell(1, 1).Style.Alignment.WrapText = true;
        ws.Range(1, 1, 26, 8).Merge();
        ws.Column(1).Width = 100;
        ws.Row(1).Height = 540;
    }

    private static void AddSurveyTemplatesSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveyTemplates);
        WriteHeader(ws, "TemplateKey", "NameAr", "NameEn", "DescriptionAr", "DescriptionEn");
        if (includeSamples)
        {
            var rows = SampleSurveyTemplatesUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Key, r.Ar, r.En, r.Da, r.De);
            }
        }
    }

    private static void AddSurveyTemplateQuestionsSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveyTemplateQuestions);
        WriteHeader(ws, "TemplateKey", "DisplayOrder", "QuestionType", "TitleAr", "TitleEn", "IsRequired", "OptionsJson");
        if (includeSamples)
        {
            var rows = SampleSurveyTemplateQuestionsUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Tk, r.Ord, r.Qt, r.Tar, r.Ten, r.Req, r.Opt);
            }
        }
    }

    private static void AddDepartmentsSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetDepartments);
        WriteHeader(ws, "Code", "NameAr", "NameEn", "ParentDepartmentCode");
        if (includeSamples)
        {
            var rows = SampleDepartmentsUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Code, r.Ar, r.En, r.Parent);
            }
        }
    }

    private static void AddEmployeesSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetEmployees);
        WriteHeader(ws, "EmployeeNumber", "NameAr", "NameEn", "Email", "PhoneNumber", "JobTitleAr", "JobTitleEn", "DepartmentCode", "IsActive");
        if (includeSamples)
        {
            var people = SampleEmployeesUae();
            for (var i = 0; i < people.Length; i++)
            {
                var n = i + 1;
                var p = people[i];
                WriteRow(
                    ws,
                    n + 1,
                    $"IMP-E{n:D3}",
                    p.Ar,
                    p.En,
                    p.Email,
                    p.Phone,
                    p.JobAr,
                    p.JobEn,
                    p.Dep,
                    "TRUE");
            }
        }
    }

    private static void AddPartnersSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetPartners);
        WriteHeader(ws, "Code", "NameAr", "NameEn", "Type", "Email", "PhoneNumber", "ContactPerson", "Address", "DepartmentCode", "IsActive");
        if (includeSamples)
        {
            var partners = SamplePartnersUae();
            for (var i = 0; i < partners.Length; i++)
            {
                var n = i + 1;
                var x = partners[i];
                WriteRow(
                    ws,
                    n + 1,
                    $"IMP-P{n:D3}",
                    x.Ar,
                    x.En,
                    x.Type,
                    x.Email,
                    x.Phone,
                    x.Contact,
                    x.Addr,
                    x.Dep,
                    "TRUE");
            }
        }
    }

    private static void AddUsersSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetUsers);
        WriteHeader(ws, "UserName", "Email", "Password", "NameAr", "NameEn", "EmployeeNumber", "RoleNames", "IsActive");
        if (includeSamples)
        {
            var roles = SampleUserRolesUae();
            var display = SampleUserDisplayUae();
            for (var i = 0; i < display.Length; i++)
            {
                var n = i + 1;
                var d = display[i];
                WriteRow(
                    ws,
                    n + 1,
                    $"imp.usr.{n:D2}",
                    $"imp.usr.{n:D2}@uaesample.gov.ae",
                    "ChangeMe123!",
                    d.Ar,
                    d.En,
                    $"IMP-E{n:D3}",
                    roles[i],
                    "TRUE");
            }
        }
    }

    private static void AddSurveysSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveys);
        WriteHeader(ws, "SurveyKey", "TitleAr", "TitleEn", "DescriptionAr", "DescriptionEn", "AudienceScope", "PublicCode", "ShowOnPublicPortal");
        if (includeSamples)
        {
            var surveys = SampleSurveysUae();
            for (var i = 0; i < surveys.Length; i++)
            {
                var s = surveys[i];
                WriteRow(ws, i + 2, s.Key, s.Ar, s.En, s.Da, s.De, s.Scope, string.Empty, "FALSE");
            }
        }
    }

    private static void AddSurveyQuestionsSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveyQuestions);
        WriteHeader(ws, "SurveyKey", "DisplayOrder", "QuestionType", "TitleAr", "TitleEn", "IsRequired", "OptionsJson");
        if (includeSamples)
        {
            var rows = SampleSurveyQuestionsUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Sk, r.Ord, r.Qt, r.Tar, r.Ten, r.Req, r.Opt);
            }
        }
    }

    private static void AddSurveyResponsesSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveyResponses);
        WriteHeader(ws, "SurveyKey", "UserName", "QuestionDisplayOrder", "AnswerValue");
        if (includeSamples)
        {
            var rows = SampleSurveyResponsesUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Sk, r.User, r.Ord, r.Ans);
            }
        }
    }

    private static void AddRecommendationsSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetRecommendations);
        WriteHeader(
            ws,
            "TitleAr",
            "TitleEn",
            "DescriptionAr",
            "DescriptionEn",
            "Priority",
            "SurveyKey",
            "AssignedToUserName",
            "DueDateUtc");
        if (includeSamples)
        {
            var rows = SampleRecommendationsUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Ar, r.En, r.DesAr, r.DesEn, r.Pri, r.Sk, r.Usr, r.Due);
            }
        }
    }

    private static void AddActionPlansSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetActionPlans);
        WriteHeader(
            ws,
            "ActionPlanKey",
            "TitleAr",
            "TitleEn",
            "DescriptionAr",
            "DescriptionEn",
            "SurveyKey",
            "OwnerUserName",
            "StartDateUtc",
            "EndDateUtc");
        if (includeSamples)
        {
            var rows = SampleActionPlansUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Key, r.Ar, r.En, r.DesAr, r.DesEn, r.Sk, r.Usr, r.Sd, r.Ed);
            }
        }
    }

    private static void AddInitiativesSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetInitiatives);
        WriteHeader(
            ws,
            "ActionPlanKey",
            "TitleAr",
            "TitleEn",
            "DescriptionAr",
            "DescriptionEn",
            "OwnerUserName",
            "TargetDateUtc");
        if (includeSamples)
        {
            var rows = SampleInitiativesUae();
            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Ap, r.Ar, r.En, r.DesAr, r.DesEn, r.Usr, r.Td);
            }
        }
    }

    private static void WriteHeader(IXLWorksheet ws, params string[] cols)
    {
        for (var i = 0; i < cols.Length; i++)
        {
            ws.Cell(1, i + 1).Value = cols[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }
    }

    private static void WriteRow(IXLWorksheet ws, int row, params string[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            ws.Cell(row, i + 1).Value = values[i];
        }
    }
}
