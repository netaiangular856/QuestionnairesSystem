using ClosedXML.Excel;

namespace QuestionnairesSystem.Application.Features.DataBulk;

internal static class ExcelBulkTemplateBuilder
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
            "7) SurveyResponses — إجابات (SurveyKey، UserName، QuestionDisplayOrder، AnswerValue).\r\n\r\n" +
            "AudienceScope (Surveys): 1 Everyone, 2 Guest, 3 SpecificUsers, 4 AllOrganizationMembers.\r\n" +
            "QuestionType (SurveyQuestions و SurveyTemplateQuestions): 1 ShortText, 2 LongText, 3 SingleChoice, 4 MultipleChoice, 5 Rating, 6 Scale, 7 YesNo, 8 Date, 9 Number.\r\n" +
            "OptionsJson للاختيارات: [{\"value\":\"a\",\"labelAr\":\"خيار\",\"labelEn\":\"Option\"}] — وللمقياس مثلاً {\"min\":1,\"max\":5}.\r\n" +
            "عمود SurveyKey في SurveyResponses يجب أن يطابق قيمة Code المحفوظة للاستبيان (= PublicCode إن وُجد، وإلا SurveyKey في ورقة Surveys).\r\n" +
            "أمثلة الصفوف تستخدم بادئة IMP- لتقليل التصادم مع بياناتك؛ احذفها أو غيّر الأكواد قبل الإنتاج.\r\n" +
            "القالب الفارغ: صف العناوين فقط. القالب مع أمثلة: صفوف توضيحية يمكن حذفها أو استبدالها.\r\n";
        ws.Cell(1, 1).Style.Alignment.WrapText = true;
        ws.Range(1, 1, 22, 8).Merge();
        ws.Column(1).Width = 100;
        ws.Row(1).Height = 460;
    }

    private static void AddSurveyTemplatesSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveyTemplates);
        WriteHeader(ws, "TemplateKey", "NameAr", "NameEn", "DescriptionAr", "DescriptionEn");
        if (includeSamples)
        {
            var rows = new (string Key, string Ar, string En, string Da, string De)[]
            {
                ("IMP-TPL-01", "قالب — تقييم أسبوعي", "Template — weekly check-in", "نسخة مختصرة للفرق", "Short team pulse"),
                ("IMP-TPL-02", "قالب — ورشة تدريب", "Template — training workshop", "بعد الحضور", "Post-training"),
                ("IMP-TPL-03", "قالب — زيارة عميل", "Template — client visit", "ميداني", "Field"),
                ("IMP-TPL-04", "قالب — ضيف", "Template — guest", "بدون حساب", "No account"),
                ("IMP-TPL-05", "قالب — مبيعات", "Template — sales", "أهداف الربع", "Quarter goals"),
                ("IMP-TPL-06", "قالب — خدمة عملاء", "Template — customer care", "مركز الاتصال", "Call center"),
                ("IMP-TPL-07", "قالب — رضا موظف", "Template — employee CSAT", "سريع", "Quick"),
                ("IMP-TPL-08", "قالب — رفاهية", "Template — wellbeing", "رفاهية عامة", "General wellbeing"),
                ("IMP-TPL-09", "قالب — شركاء", "Template — partners", "تقييم خارجي", "External review"),
                ("IMP-TPL-10", "قالب — عام", "Template — generic", "متعدد الاستخدام", "Multi-purpose"),
            };

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
            var choiceOpts =
                "[{\"value\":\"a\",\"labelAr\":\"موافق\",\"labelEn\":\"Agree\"},{\"value\":\"b\",\"labelAr\":\"غير موافق\",\"labelEn\":\"Disagree\"}]";
            var multiOpts =
                "[{\"value\":\"x\",\"labelAr\":\"X\",\"labelEn\":\"X\"},{\"value\":\"y\",\"labelAr\":\"Y\",\"labelEn\":\"Y\"}]";
            var scaleOpts = "{\"min\":1,\"max\":5}";

            var rows = new (string Tk, string Ord, string Qt, string Tar, string Ten, string Req, string Opt)[]
            {
                ("IMP-TPL-01", "1", "1", "ما أهم ما يجب متابعته؟", "What should we track?", "TRUE", ""),
                ("IMP-TPL-02", "1", "7", "هل المحتوى مفيد؟", "Was the content useful?", "TRUE", ""),
                ("IMP-TPL-03", "1", "3", "تقييم الزيارة", "Visit score", "TRUE", choiceOpts),
                ("IMP-TPL-04", "1", "1", "ما سبب الزيارة؟", "Reason for visit?", "FALSE", ""),
                ("IMP-TPL-05", "1", "5", "مدى الثقة بالهدف", "Confidence in target", "TRUE", ""),
                ("IMP-TPL-06", "1", "2", "صف المشكلة باختصار", "Describe the issue briefly", "TRUE", ""),
                ("IMP-TPL-07", "1", "9", "عدد النقاط (0–10)", "Points 0–10", "FALSE", ""),
                ("IMP-TPL-08", "1", "8", "تاريخ آخر إجازة", "Last leave date", "FALSE", ""),
                ("IMP-TPL-09", "1", "4", "وسائل التواصل المفضلة", "Preferred channels", "TRUE", multiOpts),
                ("IMP-TPL-10", "1", "6", "جاهزية الفريق", "Team readiness", "TRUE", scaleOpts),
            };

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
            var rows = new (string Code, string Ar, string En, string Parent)[]
            {
                ("IMP-D01", "موارد بشرية", "Human Resources", ""),
                ("IMP-D02", "مالية", "Finance", ""),
                ("IMP-D03", "تقنية معلومات", "Information Technology", ""),
                ("IMP-D04", "دعم فني", "Technical Support", "IMP-D03"),
                ("IMP-D05", "مبيعات", "Sales", ""),
                ("IMP-D06", "تجزئة", "Retail", "IMP-D05"),
                ("IMP-D07", "شؤون قانونية", "Legal", ""),
                ("IMP-D08", "عقود", "Contracts", "IMP-D07"),
                ("IMP-D09", "بحث وتطوير", "Research & Development", ""),
                ("IMP-D10", "جودة", "Quality Assurance", "IMP-D09"),
            };

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
            var deps = new[] { "IMP-D01", "IMP-D02", "IMP-D03", "IMP-D04", "IMP-D05", "IMP-D06", "IMP-D07", "IMP-D08", "IMP-D09", "IMP-D10" };
            var people = new (string Ar, string En, string Email, string Phone, string JobAr, string JobEn)[]
            {
                ("أحمد محمود", "Ahmed Mahmoud", "imp.e01@template.sample", "0501000001", "محلل أعمال", "Business Analyst"),
                ("سارة علي", "Sara Ali", "imp.e02@template.sample", "0501000002", "مطوّر برمجيات", "Software Developer"),
                ("خالد عمر", "Khaled Omar", "imp.e03@template.sample", "0501000003", "مدير مشروع", "Project Manager"),
                ("ليلى حسن", "Layla Hassan", "imp.e04@template.sample", "0501000004", "مصممة واجهات", "UX Designer"),
                ("يوسف ناصر", "Youssef Nasser", "imp.e05@template.sample", "0501000005", "مهندس نظم", "Systems Engineer"),
                ("نورة إبراهيم", "Noura Ibrahim", "imp.e06@template.sample", "0501000006", "مسؤولة موارد بشرية", "HR Specialist"),
                ("طارق منصور", "Tariq Mansour", "imp.e07@template.sample", "0501000007", "محاسب", "Accountant"),
                ("هند عبدالله", "Hind Abdullah", "imp.e08@template.sample", "0501000008", "منسقة تسويق", "Marketing Coordinator"),
                ("فيصل راشد", "Faisal Rashid", "imp.e09@template.sample", "0501000009", "فني دعم", "Support Technician"),
                ("ريم سالم", "Reem Salem", "imp.e10@template.sample", "0501000010", "باحثة", "Researcher"),
            };

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
                    deps[i],
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
            var types = new[] { "1", "2", "3", "4", "99", "1", "2", "3", "4", "99" };
            var deps = new[] { "IMP-D01", "IMP-D02", "IMP-D03", "IMP-D04", "IMP-D05", "IMP-D01", "IMP-D02", "IMP-D03", "IMP-D04", "IMP-D05" };
            var partners = new (string Ar, string En, string Email, string Phone, string Contact, string Addr)[]
            {
                ("وكيل شمال", "North Dealer Agent", "imp.p01@template.sample", "0510000001", "فهد", "الرياض — حي العليا"),
                ("شريك استراتيجي", "Strategic Partner Co.", "imp.p02@template.sample", "0510000002", "نوف", "جدة — الكورنيش"),
                ("مورّد معدات", "Equipment Vendor Ltd", "imp.p03@template.sample", "0510000003", "سامي", "الدمام — الصناعية"),
                ("عميل مؤسسي", "Enterprise Customer", "imp.p04@template.sample", "0510000004", "لمى", "الخبر — النزهة"),
                ("جهة أخرى", "Other Entity", "imp.p05@template.sample", "0510000005", "عادل", "مكة — العزيزية"),
                ("موزع جنوب", "South Distributor", "imp.p06@template.sample", "0510000006", "هالة", "أبها — المفتاحة"),
                ("شريك تقني", "Technology Partner", "imp.p07@template.sample", "0510000007", "رامي", "المدينة المنورة"),
                ("بائع تجزئة", "Retail Vendor", "imp.p08@template.sample", "0510000008", "دانة", "تبوك — المروج"),
                ("عميل قطاع حكومي", "Gov Sector Client", "imp.p09@template.sample", "0510000009", "بدر", "الأحساء — الهفوف"),
                ("فرع إقليمي", "Regional Branch Partner", "imp.p10@template.sample", "0510000010", "منى", "نجران — الملك فهد"),
            };

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
                    types[i],
                    x.Email,
                    x.Phone,
                    x.Contact,
                    x.Addr,
                    deps[i],
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
            var roles = new[] { "Employee", "Employee", "Admin", "Employee", "Employee", "Employee", "Employee", "Employee", "Employee", "Admin,Employee" };
            var display = new (string Ar, string En)[]
            {
                ("مستخدم قالب 1", "Template User 01"),
                ("مستخدم قالب 2", "Template User 02"),
                ("مستخدم قالب 3", "Template User 03"),
                ("مستخدم قالب 4", "Template User 04"),
                ("مستخدم قالب 5", "Template User 05"),
                ("مستخدم قالب 6", "Template User 06"),
                ("مستخدم قالب 7", "Template User 07"),
                ("مستخدم قالب 8", "Template User 08"),
                ("مستخدم قالب 9", "Template User 09"),
                ("مستخدم قالب 10", "Template User 10"),
            };

            for (var i = 0; i < display.Length; i++)
            {
                var n = i + 1;
                var d = display[i];
                WriteRow(
                    ws,
                    n + 1,
                    $"imp.usr.{n:D2}",
                    $"imp.usr.{n:D2}@template.sample",
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
            // لا نستخدم 3 (SpecificUsers) لأن القالب لا يضم AudienceMembers — يفشل التحقق عند الإنشاء.
            var scopes = new[] { "4", "4", "1", "2", "4", "4", "4", "4", "1", "4" };
            var surveys = new (string Key, string Ar, string En, string Da, string De)[]
            {
                ("IMP-SVY-01", "رضا الموظفين Q1", "Employee satisfaction — pulse", "استطلاع أسبوعي", "Weekly pulse check"),
                ("IMP-SVY-02", "تدريب ما بعد العمل", "Post-workshop feedback", "بعد ورشة الأمن السيبراني", "After cybersecurity workshop"),
                ("IMP-SVY-03", "زيارة العميل", "Customer visit form", "تقييم زيارة ميدانية", "Field visit evaluation"),
                ("IMP-SVY-04", "ضيف — استبيان قصير", "Guest quick survey", "للزوار بدون حساب", "For guests without login"),
                ("IMP-SVY-05", "مبيعات الربع", "Quarterly sales check", "أهداف الفريق", "Team targets"),
                ("IMP-SVY-06", "متابعة داخلية", "Internal follow-up", "قائمة داخلية", "Internal follow-up round"),
                ("IMP-SVY-07", "جودة الخدمة", "Service quality", "مركز الاتصال", "Call center"),
                ("IMP-SVY-08", "التوازن والرفاهية", "Wellbeing snapshot", "بدون أسماء في التقرير", "Anonymous-style wording"),
                ("IMP-SVY-09", "شركاء — تقييم أداء", "Partner performance", "للفريق الخارجي", "External partners"),
                ("IMP-SVY-10", "استبيان عام تجريبي", "General template survey", "نسخة متعددة الاستخدام", "Reusable template"),
            };

            for (var i = 0; i < surveys.Length; i++)
            {
                var s = surveys[i];
                var pub = string.Empty;
                WriteRow(ws, i + 2, s.Key, s.Ar, s.En, s.Da, s.De, scopes[i], pub, "FALSE");
            }
        }
    }

    private static void AddSurveyQuestionsSheet(XLWorkbook wb, bool includeSamples)
    {
        var ws = wb.Worksheets.Add(SheetSurveyQuestions);
        WriteHeader(ws, "SurveyKey", "DisplayOrder", "QuestionType", "TitleAr", "TitleEn", "IsRequired", "OptionsJson");
        if (includeSamples)
        {
            var choiceOpts =
                "[{\"value\":\"a\",\"labelAr\":\"موافق\",\"labelEn\":\"Agree\"},{\"value\":\"b\",\"labelAr\":\"غير موافق\",\"labelEn\":\"Disagree\"}]";
            var multiOpts =
                "[{\"value\":\"x\",\"labelAr\":\"خيار X\",\"labelEn\":\"X\"},{\"value\":\"y\",\"labelAr\":\"خيار Y\",\"labelEn\":\"Y\"},{\"value\":\"z\",\"labelAr\":\"خيار Z\",\"labelEn\":\"Z\"}]";
            var scaleOpts = "{\"min\":1,\"max\":5}";

            var rows = new (string Sk, string Ord, string Qt, string Tar, string Ten, string Req, string Opt)[]
            {
                ("IMP-SVY-01", "1", "1", "ما أبرز ملاحظة هذا الأسبوع؟", "Top note this week?", "TRUE", ""),
                ("IMP-SVY-02", "1", "7", "هل أنصح بالورشة للزملاء؟", "Would you recommend the workshop?", "TRUE", ""),
                ("IMP-SVY-03", "1", "3", "تقييم الزيارة", "Visit rating", "TRUE", choiceOpts),
                ("IMP-SVY-04", "1", "5", "مدى الرضا (1–5)", "Satisfaction (1–5)", "FALSE", ""),
                ("IMP-SVY-05", "1", "2", "صف أكبر عائق للهدف", "Describe the main blocker", "TRUE", ""),
                ("IMP-SVY-06", "1", "9", "عدد المهام المغلقة", "Closed tasks count", "FALSE", ""),
                ("IMP-SVY-07", "1", "8", "تاريخ آخر تذكرة", "Last ticket date", "TRUE", ""),
                ("IMP-SVY-08", "1", "4", "اختر كل ما ينطبق", "Select all that apply", "TRUE", multiOpts),
                ("IMP-SVY-09", "1", "6", "درجة الجاهزية", "Readiness score", "TRUE", scaleOpts),
                ("IMP-SVY-10", "1", "1", "تعليق عام", "General comment", "FALSE", ""),
            };

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
            // SurveyKey يطابق عمود Surveys (PublicCode فارغ ⇒ Code = SurveyKey). مستخدم واحد لكل استبيان لتجنب تعارض «رد مسجل».
            var rows = new (string Sk, string User, string Ord, string Ans)[]
            {
                ("IMP-SVY-01", "imp.usr.01", "1", "تحسّن ملحوظ في التواصل الداخلي."),
                ("IMP-SVY-02", "imp.usr.02", "1", "yes"),
                ("IMP-SVY-03", "imp.usr.03", "1", "a"),
                ("IMP-SVY-04", "imp.usr.04", "1", "4"),
                ("IMP-SVY-05", "imp.usr.05", "1", "الهدف يحتاج موارد إضافية هذا الربع."),
                ("IMP-SVY-06", "imp.usr.06", "1", "12"),
                ("IMP-SVY-07", "imp.usr.07", "1", "2026-05-01"),
                ("IMP-SVY-08", "imp.usr.08", "1", "x,y"),
                ("IMP-SVY-09", "imp.usr.09", "1", "4"),
                ("IMP-SVY-10", "imp.usr.10", "1", "شكراً، القالب يعمل كما يُفترض."),
            };

            for (var i = 0; i < rows.Length; i++)
            {
                var r = rows[i];
                WriteRow(ws, i + 2, r.Sk, r.User, r.Ord, r.Ans);
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
