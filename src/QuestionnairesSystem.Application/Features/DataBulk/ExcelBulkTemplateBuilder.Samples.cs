namespace QuestionnairesSystem.Application.Features.DataBulk;

/// <summary>
/// بيانات عيّنة للقالب (سياق دولة الإمارات — 20 صفاً لكل ورقة حيث ينطبق).
/// </summary>
internal static partial class ExcelBulkTemplateBuilder
{
    private const string ChoiceOptsUae =
        "[{\"value\":\"a\",\"labelAr\":\"ممتاز — يتماشى مع رؤية الإمارات 2071\",\"labelEn\":\"Excellent — aligns with UAE Vision 2071\"}," +
        "{\"value\":\"b\",\"labelAr\":\"جيد\",\"labelEn\":\"Good\"}," +
        "{\"value\":\"c\",\"labelAr\":\"يحتاج تحسين\",\"labelEn\":\"Needs improvement\"}]";

    private const string MultiOptsUae =
        "[{\"value\":\"smart\",\"labelAr\":\"الخدمات الذكية (تطبيقات حكومية)\",\"labelEn\":\"Smart gov apps\"}," +
        "{\"value\":\"center\",\"labelAr\":\"مركز سعادة المتعامل\",\"labelEn\":\"Happiness service centre\"}," +
        "{\"value\":\"digital\",\"labelAr\":\"الهوية الرقمية / منصة موحدة\",\"labelEn\":\"Digital ID / unified portal\"}," +
        "{\"value\":\"call\",\"labelAr\":\"الخط الساخن / مركز الاتصال\",\"labelEn\":\"Hotline / call centre\"}]";

    private const string ScaleOpts1to10 = "{\"min\":1,\"max\":10}";

    private static (string Code, string Ar, string En, string Parent)[] SampleDepartmentsUae() =>
    [
        ("IMP-D01", "مكتب الاستراتيجية والتطوير المؤسسي", "Office of Strategy & Institutional Development", ""),
        ("IMP-D02", "إدارة الأمن السيبراني والبنية التحتية الرقمية", "Cybersecurity & Digital Infrastructure", ""),
        ("IMP-D03", "إدارة الموارد البشرية والتوطين", "Human Resources & Emiratisation", ""),
        ("IMP-D04", "إدارة الشؤون المالية والميزانية", "Finance & Budget", ""),
        ("IMP-D05", "إدارة الابتكار والتقنية", "Innovation & Technology", "IMP-D02"),
        ("IMP-D06", "إدارة خدمة المتعاملين وجودة التجربة", "Customer Experience & Service Quality", ""),
        ("IMP-D07", "إدارة الشؤون القانونية والامتثال", "Legal Affairs & Compliance", ""),
        ("IMP-D08", "إدارة التواصل الحكومي والعلاقات العامة", "Government Communication & Public Relations", ""),
        ("IMP-D09", "إدارة الشراكات الاستراتيجية", "Strategic Partnerships", ""),
        ("IMP-D10", "إدارة تمكين أصحاب الهمم والمجتمع", "People of Determination & Community", ""),
        ("IMP-D11", "إدارة الاستدامة والبيئة المؤسسية", "Sustainability & Corporate Environment", ""),
        ("IMP-D12", "إدارة التعليم والتدريب المهني", "Learning & Professional Development", ""),
        ("IMP-D13", "إدارة الصحة والسلامة المهنية", "Occupational Health & Safety", ""),
        ("IMP-D14", "إدارة المشتريات والعقود", "Procurement & Contracts", ""),
        ("IMP-D15", "إدارة الجودة والتميز المؤسسي", "Quality & Institutional Excellence", ""),
        ("IMP-D16", "إدارة البيانات والتحليلات", "Data & Analytics", "IMP-D05"),
        ("IMP-D17", "إدارة المشاريع والمتابعة", "Projects & Performance Follow-up", ""),
        ("IMP-D18", "إدارة المسؤولية المجتمعية", "Corporate Social Responsibility", ""),
        ("IMP-D19", "إدارة الإعلام الرقمي والمحتوى", "Digital Media & Content", "IMP-D08"),
        ("IMP-D20", "إدارة الطوارئ واستمرارية الأعمال", "Emergency & Business Continuity", ""),
    ];

    private static (string Ar, string En, string Email, string Phone, string JobAr, string JobEn, string Dep)[] SampleEmployeesUae() =>
    [
        ("محمد عبدالله المنصوري", "Mohammed Al Mansoori", "imp.e01@uaesample.gov.ae", "+971501000001", "مدير مكتب استراتيجي", "Strategy Office Director", "IMP-D01"),
        ("فاطمة سالم الكعبي", "Fatima Al Kaabi", "imp.e02@uaesample.gov.ae", "+971501000002", "رئيس قسم أمن سيبراني", "Head of Cybersecurity Section", "IMP-D02"),
        ("سعيد حمد النعيمي", "Saeed Al Nuaimi", "imp.e03@uaesample.gov.ae", "+971501000003", "مستشار توطين", "Emiratisation Advisor", "IMP-D03"),
        ("مريم أحمد الشامسي", "Mariam Al Shamsi", "imp.e04@uaesample.gov.ae", "+971501000004", "محلل ميزانية", "Budget Analyst", "IMP-D04"),
        ("عمر يوسف البلوشي", "Omar Al Balushi", "imp.e05@uaesample.gov.ae", "+971501000005", "مهندس حلول ذكية", "Smart Solutions Engineer", "IMP-D05"),
        ("شيخة علي المزروعي", "Shaikha Al Mazrouei", "imp.e06@uaesample.gov.ae", "+971501000006", "أخصائي تجربة متعامل", "Customer Experience Specialist", "IMP-D06"),
        ("حمدان سلطان السويدي", "Hamdan Al Suwaidi", "imp.e07@uaesample.gov.ae", "+971501000007", "مستشار قانوني", "Legal Counsel", "IMP-D07"),
        ("لطيفة حسن الظاهري", "Latifa Al Dhaheri", "imp.e08@uaesample.gov.ae", "+971501000008", "منسق إعلام حكومي", "Government Media Coordinator", "IMP-D08"),
        ("راشد خميس المنهلي", "Rashid Al Menhali", "imp.e09@uaesample.gov.ae", "+971501000009", "مدير شراكات", "Partnerships Manager", "IMP-D09"),
        ("نورة سعيد الكندي", "Noura Al Kindi", "imp.e10@uaesample.gov.ae", "+971501000010", "منسق إمكانية وصول", "Accessibility Coordinator", "IMP-D10"),
        ("خالد ماجد الهاجري", "Khaled Al Hajeri", "imp.e11@uaesample.gov.ae", "+971501000011", "مسؤول استدامة", "Sustainability Officer", "IMP-D11"),
        ("هند منصور العامري", "Hind Al Amiri", "imp.e12@uaesample.gov.ae", "+971501000012", "مصمم برامج تدريب", "Training Program Designer", "IMP-D12"),
        ("عبدالرحمن فيصل الزعابي", "Abdulrahman Al Zaabi", "imp.e13@uaesample.gov.ae", "+971501000013", "مسؤول سلامة مهنية", "HSE Officer", "IMP-D13"),
        ("سلمى جاسم النقبي", "Salma Al Naqbi", "imp.e14@uaesample.gov.ae", "+971501000014", "أخصائي عقود", "Contracts Specialist", "IMP-D14"),
        ("طارق إبراهيم السركال", "Tariq Al Serkal", "imp.e15@uaesample.gov.ae", "+971501000015", "مدقق جودة", "Quality Auditor", "IMP-D15"),
        ("دانة حمد بن هزيم", "Dana Bin Hezaim", "imp.e16@uaesample.gov.ae", "+971501000016", "محلل بيانات", "Data Analyst", "IMP-D16"),
        ("فيصل ناصر الرميثي", "Faisal Al Rumaithi", "imp.e17@uaesample.gov.ae", "+971501000017", "مدير مشروع حكومي", "Government Project Manager", "IMP-D17"),
        ("أمل سعيد المنصوري", "Amal Al Mansoori", "imp.e18@uaesample.gov.ae", "+971501000018", "منسق مبادرات مجتمعية", "CSR Initiatives Coordinator", "IMP-D18"),
        ("يوسف حمد البستكي", "Yousef Al Bastaki", "imp.e19@uaesample.gov.ae", "+971501000019", "منتج محتوى رقمي", "Digital Content Producer", "IMP-D19"),
        ("ميثاء عبدالعزيز الكتبي", "Maitha Al Ketbi", "imp.e20@uaesample.gov.ae", "+971501000020", "منسق طوارئ", "Emergency Coordinator", "IMP-D20"),
    ];

    private static (string Ar, string En, string Email, string Phone, string Contact, string Addr, string Type, string Dep)[] SamplePartnersUae() =>
    [
        ("موزع معتمد — أبوظبي", "Authorised distributor — Abu Dhabi", "imp.p01@uaesample.ae", "+97124100001", "خالد المنصوري", "أبوظبي — الكورنيش — برج التجارة", "1", "IMP-D09"),
        ("شريك استراتيجي — دبي", "Strategic partner — Dubai", "imp.p02@uaesample.ae", "+97144100002", "ليلى الحمادي", "دبي — الخليج التجاري — باي سكوير", "2", "IMP-D09"),
        ("مورّد حلول سحابية", "Cloud solutions vendor", "imp.p03@uaesample.ae", "+97165100003", "سالم الكعبي", "الشارقة — التكنولوجيا — المنطقة الحرة", "3", "IMP-D05"),
        ("جهة حكومية شريكة", "Government partner entity", "imp.p04@uaesample.ae", "+97172100004", "نورة الشامسي", "عجمان — الحميدية — مبنى الخدمات", "4", "IMP-D06"),
        ("مكتب استشارات — رأس الخيمة", "Consulting office — RAK", "imp.p05@uaesample.ae", "+97172100005", "راشد النعيمي", "رأس الخيمة — النخيل — شارع الميناء", "99", "IMP-D01"),
        ("معرض تجزئة — الفجيرة", "Retail showroom — Fujairah", "imp.p06@uaesample.ae", "+97192100006", "هند البلوشي", "الفجيرة — الميناء — الدائري", "1", "IMP-D14"),
        ("شريك تقني — أم القيوين", "Technology partner — UAQ", "imp.p07@uaesample.ae", "+97167100007", "عبدالله السويدي", "أم القيوين — المدينة القديمة", "2", "IMP-D05"),
        ("مورد معدات مكاتب", "Office equipment supplier", "imp.p08@uaesample.ae", "+97144100008", "فاطمة المزروعي", "دبي — جبل علي — المنطقة الصناعية", "3", "IMP-D14"),
        ("عميل قطاع ضيافة", "Hospitality sector client", "imp.p09@uaesample.ae", "+97124100009", "سلطان الهاجري", "أبوظبي — جزيرة السعديات", "4", "IMP-D06"),
        ("وكيل خدمات لوجستية", "Logistics services agent", "imp.p10@uaesample.ae", "+97165100010", "مريم الكندي", "الشارقة — المطار الحرة", "1", "IMP-D17"),
        ("شريك تدريب معتمد", "Accredited training partner", "imp.p11@uaesample.ae", "+97144100011", "حمدان الرميثي", "دبي — المعرفة — مجمع الأعمال", "2", "IMP-D12"),
        ("مورّد طاقة شمسية", "Solar energy vendor", "imp.p12@uaesample.ae", "+97124100012", "شيخة الظاهري", "أبوظبي — مصدر — المدينة المستدامة", "3", "IMP-D11"),
        ("شركة اتصالات", "Telecom company", "imp.p13@uaesample.ae", "+97180001314", "خدمة عملاء الاتصالات", "دولة الإمارات — مركز الاتصال الوطني", "3", "IMP-D02"),
        ("مستشفى خاص — دبي", "Private hospital — Dubai", "imp.p14@uaesample.ae", "+97144100014", "د. أحمد المنصوري", "دبي — الخليج التجاري", "4", "IMP-D13"),
        ("بنك شريك", "Partner bank", "imp.p15@uaesample.ae", "+97160001515", "العلاقات المؤسسية", "أبوظبي — شارع الكورنيش", "2", "IMP-D04"),
        ("جهة إعلامية", "Media organisation", "imp.p16@uaesample.ae", "+97124100016", "لطيفة النقبي", "أبوظبي — منطقة الإعلام", "99", "IMP-D08"),
        ("جامعة — شريك بحث", "University research partner", "imp.p17@uaesample.ae", "+97165100017", "د. عمر العامري", "الشارقة — الحرم الجامعي", "2", "IMP-D16"),
        ("مطور عقاري", "Real estate developer", "imp.p18@uaesample.ae", "+97144100018", "فيصل البستكي", "دبي — دبي لاند", "4", "IMP-D17"),
        ("شركة نقل ذكي", "Smart mobility company", "imp.p19@uaesample.ae", "+97124100019", "ميثاء السركال", "أبوظبي — جزيرة الريم", "1", "IMP-D05"),
        ("مؤسسة خيرية", "Charitable foundation", "imp.p20@uaesample.ae", "+97124100020", "أمل الكتبي", "أبوظبي — زايد للإنسانية", "99", "IMP-D18"),
    ];

    private static string[] SampleUserRolesUae() =>
    [
        "Employee", "Employee", "Admin", "Employee", "Employee",
        "Employee", "Employee", "Employee", "Employee", "Employee",
        "Employee", "Employee", "Employee", "Employee", "Employee",
        "Employee", "Employee", "Employee", "Employee", "Admin,Employee",
    ];

    private static (string Ar, string En)[] SampleUserDisplayUae() =>
    [
        ("موظف — محمد المنصوري", "Staff — Mohammed Al Mansoori"),
        ("موظف — فاطمة الكعبي", "Staff — Fatima Al Kaabi"),
        ("مسؤول نظام — سعيد النعيمي", "System admin — Saeed Al Nuaimi"),
        ("موظف — مريم الشامسي", "Staff — Mariam Al Shamsi"),
        ("موظف — عمر البلوشي", "Staff — Omar Al Balushi"),
        ("موظف — شيخة المزروعي", "Staff — Shaikha Al Mazrouei"),
        ("موظف — حمدان السويدي", "Staff — Hamdan Al Suwaidi"),
        ("موظف — لطيفة الظاهري", "Staff — Latifa Al Dhaheri"),
        ("موظف — راشد المنهلي", "Staff — Rashid Al Menhali"),
        ("موظف — نورة الكندي", "Staff — Noura Al Kindi"),
        ("موظف — خالد الهاجري", "Staff — Khaled Al Hajeri"),
        ("موظف — هند العامري", "Staff — Hind Al Amiri"),
        ("موظف — عبدالرحمن الزعابي", "Staff — Abdulrahman Al Zaabi"),
        ("موظف — سلمى النقبي", "Staff — Salma Al Naqbi"),
        ("موظف — طارق السركال", "Staff — Tariq Al Serkal"),
        ("موظف — دانة بن هزيم", "Staff — Dana Bin Hezaim"),
        ("موظف — فيصل الرميثي", "Staff — Faisal Al Rumaithi"),
        ("موظف — أمل المنصوري", "Staff — Amal Al Mansoori"),
        ("موظف — يوسف البستكي", "Staff — Yousef Al Bastaki"),
        ("موظف — ميثاء الكتبي", "Staff — Maitha Al Ketbi"),
    ];

    private static (string Key, string Ar, string En, string Da, string De)[] SampleSurveyTemplatesUae() =>
    [
        ("IMP-TPL-01", "قالب — رضا متعاملي الهيئة (أسبوعي)", "Template — weekly entity satisfaction", "قياس سريع بعد زيارة مركز سعادة", "Quick pulse after happiness centre visit"),
        ("IMP-TPL-02", "قالب — ورشة «مهارات المستقبل»", "Template — Future Skills workshop", "بعد برنامج توطين المهارات", "Post Emiratisation skills session"),
        ("IMP-TPL-03", "قالب — تقييم شراكة استراتيجية", "Template — strategic partnership review", "جهات حكومية وخاصة في الإمارات", "Gov & private UAE partners"),
        ("IMP-TPL-04", "قالب — زائر / ضيف (خدمة فورية)", "Template — guest instant feedback", "بدون حساب — مناسبات وطنية", "No login — national events"),
        ("IMP-TPL-05", "قالب — مؤشرات الأداء الربعي", "Template — quarterly KPI check", "متابعة أهداف رؤية الإمارات 2071", "UAE Vision 2071 goal tracking"),
        ("IMP-TPL-06", "قالب — مركز الاتصال الحكومي 800", "Template — government 800 contact centre", "جودة الرد والوقت", "Response quality & time"),
        ("IMP-TPL-07", "قالب — ولاء موظفي الجهة", "Template — entity employee loyalty", "رفاهية وتمكين بيئة العمل", "Wellbeing & workplace enablement"),
        ("IMP-TPL-08", "قالب — استدامة ومبادرة «نحن الإمارات»", "Template — sustainability & We UAE", "ممارسات خضراء في المكتب", "Green office practices"),
        ("IMP-TPL-09", "قالب — مورد معتمد للمشتريات", "Template — approved vendor survey", "تقييم التزام العقود الحكومية", "Gov contract compliance"),
        ("IMP-TPL-10", "قالب — تدريب على الذكاء الاصطناعي الحكومي", "Template — gov AI literacy training", "بعد جلسة «الحكومة الذكية»", "Post smart government session"),
        ("IMP-TPL-11", "قالب — خدمات ذوي الإعاقة", "Template — people of determination services", "إمكانية الوصول في المبنى", "Building accessibility"),
        ("IMP-TPL-12", "قالب — فعالية اليوم الوطني", "Template — National Day event", "تجربة الزوار في الميدان", "Visitor field experience"),
        ("IMP-TPL-13", "قالب — برنامج المشاريع الصغيرة", "Template — SME support programme", "دعم ريادة الأعمال محلياً", "Local entrepreneurship support"),
        ("IMP-TPL-14", "قالب — سلامة الطوارئ والجاهزية", "Template — emergency readiness", "تجربة تدريب الإخلاء", "Evacuation drill feedback"),
        ("IMP-TPL-15", "قالب — منصة الخدمات الموحدة", "Template — unified services portal", "سهولة إكمال المعاملة إلكترونياً", "Ease of e-transaction"),
        ("IMP-TPL-16", "قالب — مجلس استشاري مجتمعي", "Template — community advisory board", "مشاركة أصحاب المصلحة", "Stakeholder engagement"),
        ("IMP-TPL-17", "قالب — زيارة ميدانية لمشروع حكومي", "Template — gov project site visit", "شفافية التنفيذ والجودة", "Execution transparency & quality"),
        ("IMP-TPL-18", "قالب — برنامج التحول الرقمي", "Template — digital transformation programme", "اعتماد الأنظمة الجديدة", "New systems adoption"),
        ("IMP-TPL-19", "قالب — مبادرة السعادة المؤسسية", "Template — institutional happiness initiative", "مؤشرات سعادة الفرق", "Team happiness indicators"),
        ("IMP-TPL-20", "قالب — تقييم عام متعدد الاستخدام", "Template — generic multi-use", "للاختبار السريع داخل الجهة", "Quick in-entity testing"),
    ];

    /// <summary>سؤال واحد لكل قالب؛ أنواع الأسئلة تتكرر لتغطية 1–9 ثم تكميل.</summary>
    private static (string Tk, string Ord, string Qt, string Tar, string Ten, string Req, string Opt)[] SampleSurveyTemplateQuestionsUae() =>
    [
        ("IMP-TPL-01", "1", "1", "ما أبرز نقطة إيجابية في خدمتك اليوم؟", "What was the main positive today?", "TRUE", ""),
        ("IMP-TPL-02", "1", "2", "كيف يمكن تحسين محتوى الورشة لموظفي الإمارات؟", "How can the workshop better serve UAE staff?", "TRUE", ""),
        ("IMP-TPL-03", "1", "3", "تقييم جودة التعاون مع الشريك", "Rate partnership quality", "TRUE", ChoiceOptsUae),
        ("IMP-TPL-04", "1", "4", "ما القنوات التي استخدمتها للوصول للخدمة؟", "Which channels did you use?", "FALSE", MultiOptsUae),
        ("IMP-TPL-05", "1", "5", "مدى ثقتك بتحقيق المؤشر الربعي", "Confidence in quarterly KPI", "TRUE", ""),
        ("IMP-TPL-06", "1", "6", "مدى رضاك عن وضوح المعلومات (1–10)", "Clarity of information (1–10)", "TRUE", ScaleOpts1to10),
        ("IMP-TPL-07", "1", "7", "هل تشعر بالدعم في بيئة العمل الإماراتية؟", "Do you feel supported at work?", "TRUE", ""),
        ("IMP-TPL-08", "1", "8", "تاريخ آخر مبادرة خضراء في مكتبك", "Date of last green initiative", "FALSE", ""),
        ("IMP-TPL-09", "1", "9", "عدد الشكاوى المغلقة هذا الشهر", "Closed complaints this month", "FALSE", ""),
        ("IMP-TPL-10", "1", "1", "ملخص سريع لما تعلمته عن الذكاء الاصطناعي", "Short summary of AI learning", "TRUE", ""),
        ("IMP-TPL-11", "1", "2", "صف تجربة الوصول للمبنى", "Describe building access experience", "TRUE", ""),
        ("IMP-TPL-12", "1", "3", "تقييم تنظيم الفعالية الوطنية", "National event organisation rating", "TRUE", ChoiceOptsUae),
        ("IMP-TPL-13", "1", "4", "ما البرامج التي تهم مشروعك؟", "Which programmes matter to you?", "TRUE", MultiOptsUae),
        ("IMP-TPL-14", "1", "5", "تقييم جاهزية فريق الطوارئ", "Emergency team readiness", "TRUE", ""),
        ("IMP-TPL-15", "1", "6", "سهولة إتمام الخطوات على المنصة (1–10)", "Ease of steps on portal (1–10)", "TRUE", ScaleOpts1to10),
        ("IMP-TPL-16", "1", "7", "هل تمت مشاركة رأيك في المجلس؟", "Was your voice heard in the board?", "TRUE", ""),
        ("IMP-TPL-17", "1", "8", "تاريخ الزيارة الميدانية", "Site visit date", "TRUE", ""),
        ("IMP-TPL-18", "1", "9", "عدد الأنظمة التي اعتمدتها فرقتك", "Systems adopted by your team", "FALSE", ""),
        ("IMP-TPL-19", "1", "1", "كلمة عن السعادة في العمل الحكومي", "Word on happiness in gov work", "FALSE", ""),
        ("IMP-TPL-20", "1", "3", "تقييم عام سريع", "Quick general rating", "TRUE", ChoiceOptsUae),
    ];

    private static (string Key, string Ar, string En, string Da, string De, string Scope)[] SampleSurveysUae() =>
    [
        ("IMP-SVY-01", "استبيان شامل — تجربة الخدمة الحكومية في الإمارات", "Comprehensive — UAE government service experience", "يضم تسعة أنواع أسئلة (نموذج مرجعي للاستيراد)", "Nine question types (reference import model)", "4"),
        ("IMP-SVY-02", "استطلاع — رضا متعاملي مركز سعادة (دبي)", "Satisfaction — Dubai happiness centre visitors", "قياس وقت الانتظار والوضوح", "Wait time & clarity", "4"),
        ("IMP-SVY-03", "استطلاع — خدمات أبوظبي الحكومية الرقمية", "Survey — Abu Dhabi digital gov services", "تطبيقات وتجربة المستخدم", "Apps & UX", "1"),
        ("IMP-SVY-04", "استبيان ضيف — فعالية اليوم الوطني", "Guest — National Day event", "بدون تسجيل — مدخلات سريعة", "No login — quick input", "2"),
        ("IMP-SVY-05", "متابعة — برنامج توطين المهارات (نافس)", "Follow-up — skills & Nafis programme", "التدريب والتوظيف", "Training & employment", "4"),
        ("IMP-SVY-06", "تقييم — شراكة استراتيجية مع القطاع الخاص", "Assessment — private sector partnership", "الالتزام وجودة التسليم", "Commitment & delivery quality", "4"),
        ("IMP-SVY-07", "استطلاع — مركز الاتصال الحكومي 800", "Survey — government 800 call centre", "اللغة العربية والإنجليزية", "Arabic & English service", "4"),
        ("IMP-SVY-08", "رفاهية موظفي الجهة — الإمارات", "Entity staff wellbeing — UAE", "التوازن والدعم النفسي", "Balance & support", "4"),
        ("IMP-SVY-09", "استدامة — مبادرة «نحن الإمارات» للمكاتب", "Sustainability — We UAE office initiative", "ترشيد استهلاك الموارد", "Resource efficiency", "1"),
        ("IMP-SVY-10", "موردون — الامتثال لعقود المشتريات", "Vendors — procurement contract compliance", "التسليم والفوترة", "Delivery & invoicing", "4"),
        ("IMP-SVY-11", "الحكومة الذكية — اعتماد أداة جديدة", "Smart government — new tool adoption", "سهولة التعلم للموظف", "Ease of learning for staff", "4"),
        ("IMP-SVY-12", "خدمات أصحاب الهمم — إمكانية الوصول", "People of determination — accessibility", "المواقف والممرات والموظفين", "Parking, routes & staff", "1"),
        ("IMP-SVY-13", "المشاريع الصغيرة — دعم ريادة الأعمال", "SME — entrepreneurship support", "الإجراءات والرسوم", "Procedures & fees", "4"),
        ("IMP-SVY-14", "الطوارئ — تدريب الإخلاء السنوي", "Emergency — annual evacuation drill", "الإرشادات والتنظيم", "Guidance & organisation", "4"),
        ("IMP-SVY-15", "منصة موحدة — إكمال معاملة إلكترونية", "Unified portal — e-transaction completion", "الخطوات والمستندات", "Steps & documents", "1"),
        ("IMP-SVY-16", "مجلس مجتمعي — مشاركة أصحاب المصلحة", "Community board — stakeholder input", "الشفافية والتأثير", "Transparency & impact", "4"),
        ("IMP-SVY-17", "زيارة ميدانية — مشروع بنية تحتية", "Site visit — infrastructure project", "السلامة والتقدم", "Safety & progress", "4"),
        ("IMP-SVY-18", "التحول الرقمي — تبني نظام الموارد البشرية", "Digital transformation — HR system rollout", "التدريب والدعم الفني", "Training & tech support", "4"),
        ("IMP-SVY-19", "السعادة المؤسسية — فريق العمل", "Institutional happiness — teamwork", "التقدير والتعاون", "Recognition & cooperation", "1"),
        ("IMP-SVY-20", "استبيان عام — ملاحظات تحسين سريعة", "General — quick improvement notes", "أي جهة حكومية في الدولة", "Any UAE government entity", "4"),
    ];

    /// <summary>
    /// 20 صفاً: الصفوف 1–9 لـ IMP-SVY-01 (كل نوع سؤال مرة واحدة)، الصفوف 10–20 لاستبيانات IMP-SVY-02..12 بسؤال واحد لكل منها.
    /// </summary>
    private static (string Sk, string Ord, string Qt, string Tar, string Ten, string Req, string Opt)[] SampleSurveyQuestionsUae() =>
    [
        ("IMP-SVY-01", "1", "1", "ما أكثر ما أعجبك في تعامل موظف الخدمة؟", "What impressed you most about the staff?", "TRUE", ""),
        ("IMP-SVY-01", "2", "2", "صف بالتفصيل كيف يمكن جعل الخدمة أقرب لرؤية الإمارات 2071", "Describe how the service can align better with UAE Vision 2071", "TRUE", ""),
        ("IMP-SVY-01", "3", "3", "كيف تقيم وضوح الإجراءات المقدمة؟", "How clear were the procedures?", "TRUE", ChoiceOptsUae),
        ("IMP-SVY-01", "4", "4", "ما القنوات التي استخدمتها؟ (اختر كل ما ينطبق)", "Which channels did you use?", "TRUE", MultiOptsUae),
        ("IMP-SVY-01", "5", "5", "مدى رضاك العام (1–5)", "Overall satisfaction (1–5)", "TRUE", ""),
        ("IMP-SVY-01", "6", "6", "قيم سهولة إتمام المعاملة على مقياس 1–10", "Rate ease of completing the transaction (1–10)", "TRUE", ScaleOpts1to10),
        ("IMP-SVY-01", "7", "7", "هل تنصح زميلاً باستخدام هذه الخدمة؟", "Would you recommend this service to a colleague?", "TRUE", ""),
        ("IMP-SVY-01", "8", "8", "تاريخ آخر زيارة لك لمركز الخدمة", "Date of your last visit to the service centre", "TRUE", ""),
        ("IMP-SVY-01", "9", "9", "كم دقيقة استغرقت المعاملة تقريباً؟", "Approx. minutes spent on the transaction?", "FALSE", ""),
        ("IMP-SVY-02", "1", "1", "ملاحظة قصيرة عن وقت الانتظار", "Short note on waiting time", "TRUE", ""),
        ("IMP-SVY-03", "1", "5", "تقييم تجربة التطبيق الحكومي", "Rating for the government app experience", "TRUE", ""),
        ("IMP-SVY-04", "1", "3", "تقييم سريع للفعالية", "Quick event rating", "TRUE", ChoiceOptsUae),
        ("IMP-SVY-05", "1", "7", "هل البرنامج ساعدك مهنياً؟", "Did the programme help your career?", "TRUE", ""),
        ("IMP-SVY-06", "1", "2", "صف مستوى التزام الشريك التعاقدي", "Describe the partner’s contractual commitment", "TRUE", ""),
        ("IMP-SVY-07", "1", "6", "مدى رضاك عن لغة الرد (1–10)", "Satisfaction with response language (1–10)", "TRUE", ScaleOpts1to10),
        ("IMP-SVY-08", "1", "4", "ما مصادر الضغط التي تواجهها؟", "What pressure sources do you face?", "TRUE", MultiOptsUae),
        ("IMP-SVY-09", "1", "8", "تاريخ المشاركة في نشاط الاستدامة", "Date of sustainability activity participation", "FALSE", ""),
        ("IMP-SVY-10", "1", "9", "عدد أوامر الشراء المستلمة هذا الربع", "Purchase orders received this quarter", "FALSE", ""),
        ("IMP-SVY-11", "1", "1", "أهم صعوبة واجهتها مع الأداة الجديدة", "Main difficulty with the new tool", "TRUE", ""),
        ("IMP-SVY-12", "1", "3", "تقييم سهولة الوصول للمبنى", "Building access ease rating", "TRUE", ChoiceOptsUae),
    ];

    /// <summary>20 صف إجابة: 9 لـ IMP-SVY-01 (نفس المستخدم)، ثم 11 استبياناً بمستخدم مختلف لكل منها.</summary>
    private static (string Sk, string User, string Ord, string Ans)[] SampleSurveyResponsesUae() =>
    [
        ("IMP-SVY-01", "imp.usr.01", "1", "التعامل بلغة واضحة وابتسامة ترحيبية."),
        ("IMP-SVY-01", "imp.usr.01", "2", "يمكن تقليل الخطوات الورقية وتعزيز التكامل مع الهوية الرقمية لتجربة أسرع لمتعاملي دولة الإمارات."),
        ("IMP-SVY-01", "imp.usr.01", "3", "a"),
        ("IMP-SVY-01", "imp.usr.01", "4", "smart,center"),
        ("IMP-SVY-01", "imp.usr.01", "5", "5"),
        ("IMP-SVY-01", "imp.usr.01", "6", "9"),
        ("IMP-SVY-01", "imp.usr.01", "7", "yes"),
        ("IMP-SVY-01", "imp.usr.01", "8", "2026-11-28"),
        ("IMP-SVY-01", "imp.usr.01", "9", "18"),
        ("IMP-SVY-02", "imp.usr.02", "1", "الانتظار أقل من 15 دقيقة في فرع دبي."),
        ("IMP-SVY-03", "imp.usr.03", "1", "4"),
        ("IMP-SVY-04", "imp.usr.04", "1", "b"),
        ("IMP-SVY-05", "imp.usr.05", "1", "نعم"),
        ("IMP-SVY-06", "imp.usr.06", "1", "الشريك يلتزم بالجدول الزمني ويقدم تقارير شهرية واضحة بالعربية والإنجليزية."),
        ("IMP-SVY-07", "imp.usr.07", "1", "8"),
        ("IMP-SVY-08", "imp.usr.08", "1", "smart,digital"),
        ("IMP-SVY-09", "imp.usr.09", "1", "2026-09-21"),
        ("IMP-SVY-10", "imp.usr.10", "1", "7"),
        ("IMP-SVY-11", "imp.usr.11", "1", "حاجة لمزيد من الفيديو التعليمي بالعربية."),
        ("IMP-SVY-12", "imp.usr.12", "1", "a"),
    ];

    private static (string Ar, string En, string DesAr, string DesEn, string Pri, string Sk, string Usr, string Due)[] SampleRecommendationsUae() =>
    [
        ("تعزيز التكامل مع الهوية الرقمية الإماراتية", "Strengthen UAE Pass integration", "تقليل إعادة إدخال البيانات", "Reduce re-entry of data", "1", "IMP-SVY-01", "imp.usr.01", "2026-07-15"),
        ("تحسين دليل المتعامل بالعربية والإنجليزية", "Improve bilingual customer guide", "توحيد المصطلحات الحكومية", "Standardise gov terms", "2", "IMP-SVY-02", "imp.usr.02", "2026-08-01"),
        ("توسيع قنوات الدعم لمركز الاتصال 800", "Expand 800 contact centre channels", "دردشة وواتساب معتمد", "Chat & approved WhatsApp", "1", "IMP-SVY-07", "imp.usr.03", "2026-06-30"),
        ("مبادرة رفاهية بعد رمضان للموظفين", "Post-Ramadan staff wellbeing", "جلسات دعم نفسي مجانية", "Free counselling sessions", "3", "IMP-SVY-08", "imp.usr.04", "2026-05-20"),
        ("تسريع اعتماد موردي المشتريات الإلكترونية", "Faster e-procurement vendor onboarding", "ربط مع منصة العقود", "Link to contracts platform", "2", "IMP-SVY-10", "", "2026-09-10"),
        ("خطة توعية بالذكاء الاصطناعي المسؤول", "Responsible AI awareness plan", "لجميع مديري الإدارات", "For all dept heads", "1", "IMP-SVY-11", "imp.usr.05", "2026-10-01"),
        ("تحسين وصول ذوي الإعاقة للمواقف", "Improve POD parking access", "إشارات أوضح في أبوظبي", "Clearer signage in Abu Dhabi", "2", "IMP-SVY-12", "imp.usr.06", "2026-07-22"),
        ("تبسيط إجراءات دعم المشاريع الصغيرة", "Simplify SME support procedures", "خفض المستندات المكررة", "Fewer duplicate documents", "1", "IMP-SVY-13", "imp.usr.07", "2026-08-15"),
        ("تقرير شفافية بعد تدريب الطوارئ", "Transparency report after emergency drill", "نشر النتائج داخلياً", "Publish results internally", "3", "IMP-SVY-14", "imp.usr.08", "2026-06-05"),
        ("تحسين مسار رفع المرفقات في المنصة الموحدة", "Better attachment flow on unified portal", "اختبار مع مستخدمين حقيقيين", "Test with real users", "2", "IMP-SVY-15", "imp.usr.09", "2026-07-01"),
        ("متابعة توصيات المجلس المجتمعي", "Follow up community board recommendations", "جدول زمني ربعي", "Quarterly timeline", "1", "IMP-SVY-16", "imp.usr.10", "2026-12-01"),
        ("توحيد تقارير الزيارات الميدانية", "Standardise site visit reports", "قالب وزاري واحد", "Single ministry template", "2", "IMP-SVY-17", "", "2026-08-30"),
        ("دعم فني إضافي لنظام الموارد البشرية", "Extra HR system support", "أسبوع تمكين بعد الإطلاق", "Enablement week post go-live", "1", "IMP-SVY-18", "imp.usr.11", "2026-09-15"),
        ("برنامج تقدير للفرق المتميزة", "Recognition programme for teams", "ربط بالسعادة المؤسسية", "Link to institutional happiness", "3", "IMP-SVY-19", "imp.usr.12", "2026-11-02"),
        ("حوكمة ملاحظات الاستبيان العام", "Governance for general survey feedback", "تصنيف تلقائي للملاحظات", "Auto-tag feedback", "2", "IMP-SVY-20", "imp.usr.13", "2026-06-25"),
        ("تعزيز الشراكة مع الجامعات الوطنية", "Boost partnership with national universities", "بحث مشترك في الابتكار", "Joint innovation research", "1", "IMP-SVY-06", "imp.usr.14", "2027-01-10"),
        ("تحسين مؤشرات الاستدامة في المباني", "Improve sustainability KPIs in buildings", "استهلاك المياه والطاقة", "Water & energy use", "2", "IMP-SVY-09", "imp.usr.15", "2026-10-20"),
        ("خطة توطين لمهام تقنية محددة", "Emiratisation plan for specific tech roles", "بالتنسيق مع البرامج الاتحادية", "Align with federal programmes", "1", "IMP-SVY-05", "imp.usr.16", "2026-07-30"),
        ("توسيع استبيان الضيوف في الفعاليات", "Expand guest surveys at events", "QR في كل موقع", "QR at every venue", "3", "IMP-SVY-04", "imp.usr.17", "2026-12-12"),
        ("مراجعة دورية لعقود الموردين", "Periodic vendor contract review", "لجنة مشتركة ربع سنوية", "Quarterly joint committee", "2", "IMP-SVY-10", "imp.usr.18", "2026-09-01"),
    ];

    private static (string Key, string Ar, string En, string DesAr, string DesEn, string Sk, string Usr, string Sd, string Ed)[] SampleActionPlansUae() =>
    [
        ("IMP-AP-01", "خطة — تميز تجربة متعاملي الدولة", "Plan — UAE customer experience excellence", "تحسين نقاط التماس الحكومية", "Improve gov touchpoints", "IMP-SVY-01", "imp.usr.01", "2026-05-01", "2026-12-31"),
        ("IMP-AP-02", "خطة — مراكز سعادة دبي وأبوظبي", "Plan — Dubai & Abu Dhabi happiness centres", "قياسات ربع سنوية", "Quarterly measurements", "IMP-SVY-02", "imp.usr.02", "2026-04-15", "2027-03-31"),
        ("IMP-AP-03", "خطة — التحول الرقمي الكامل للخدمات", "Plan — full digital service shift", "منصات وتكامل", "Platforms & integration", "IMP-SVY-03", "imp.usr.03", "2026-06-01", "2027-06-30"),
        ("IMP-AP-04", "خطة — فعاليات وطنية بدون احتكاك", "Plan — frictionless national events", "استبيانات ضيوف فورية", "Instant guest surveys", "IMP-SVY-04", "", "2026-11-01", "2026-12-15"),
        ("IMP-AP-05", "خطة — تمكين المواطنين في سوق العمل", "Plan — labour market enablement", "برامج مهارات ومتابعة", "Skills programmes & follow-up", "IMP-SVY-05", "imp.usr.05", "2026-05-10", "2026-11-30"),
        ("IMP-AP-06", "خطة — شراكات استراتيجية مع القطاع الخاص", "Plan — strategic private partnerships", "عقود أداء وشفافية", "Performance contracts & transparency", "IMP-SVY-06", "imp.usr.06", "2026-03-01", "2027-02-28"),
        ("IMP-AP-07", "خطة — جودة مراكز الاتصال الحكومية", "Plan — gov contact centre quality", "تدريب لغوي مستمر", "Ongoing language training", "IMP-SVY-07", "imp.usr.07", "2026-05-20", "2026-12-20"),
        ("IMP-AP-08", "خطة — رفاهية وولاء موظفي الجهة", "Plan — staff wellbeing & loyalty", "برامج صحية واجتماعية", "Health & social programmes", "IMP-SVY-08", "imp.usr.08", "2026-06-01", "2027-01-31"),
        ("IMP-AP-09", "خطة — الاستدامة الخضراء في المكاتب", "Plan — green office sustainability", "مبادرة نحن الإمارات", "We UAE initiative", "IMP-SVY-09", "imp.usr.09", "2026-04-01", "2026-12-31"),
        ("IMP-AP-10", "خطة — حوكمة الموردين والعقود", "Plan — vendor & contract governance", "تدقيق دوري", "Periodic audit", "IMP-SVY-10", "imp.usr.10", "2026-05-05", "2027-05-05"),
        ("IMP-AP-11", "خطة — اعتماد أدوات الذكاء الاصطناعي الحكومية", "Plan — adopt gov AI tools", "أخلاقيات وخصوصية", "Ethics & privacy", "IMP-SVY-11", "imp.usr.11", "2026-07-01", "2027-03-15"),
        ("IMP-AP-12", "خطة — إمكانية وصول شاملة", "Plan — universal accessibility", "معايير دولة الإمارات", "UAE accessibility standards", "IMP-SVY-12", "imp.usr.12", "2026-05-12", "2026-10-30"),
        ("IMP-AP-13", "خطة — دعم المشاريع الصغيرة محلياً", "Plan — local SME support", "إرشاد وتمويل معرفي", "Mentoring & knowledge funding", "IMP-SVY-13", "imp.usr.13", "2026-06-15", "2027-06-15"),
        ("IMP-AP-14", "خطة — جاهزية الطوارئ والاستمرارية", "Plan — emergency readiness & continuity", "تدريبات ومحاكاة", "Drills & simulation", "IMP-SVY-14", "imp.usr.14", "2026-01-01", "2026-12-31"),
        ("IMP-AP-15", "خطة — رحلة المتعامل على المنصة الموحدة", "Plan — customer journey on unified portal", "تقليل النقرات", "Reduce clicks", "IMP-SVY-15", "imp.usr.15", "2026-08-01", "2027-02-01"),
        ("IMP-AP-16", "خطة — المجالس الاستشارية المجتمعية", "Plan — community advisory boards", "مشاركة فعالة", "Effective engagement", "IMP-SVY-16", "imp.usr.16", "2026-05-25", "2026-11-25"),
        ("IMP-AP-17", "خطة — شفافية المشاريع الميدانية", "Plan — field project transparency", "تقارير شهرية مفتوحة", "Open monthly reports", "IMP-SVY-17", "", "2026-06-10", "2027-01-10"),
        ("IMP-AP-18", "خطة — التحول الرقمي للموارد البشرية", "Plan — HR digital transformation", "تبني وتدريب", "Adoption & training", "IMP-SVY-18", "imp.usr.18", "2026-07-15", "2027-07-15"),
        ("IMP-AP-19", "خطة — السعادة والثقافة المؤسسية", "Plan — happiness & culture", "فعاليات داخلية", "Internal events", "IMP-SVY-19", "imp.usr.19", "2026-05-18", "2026-12-18"),
        ("IMP-AP-20", "خطة — استغلال ملاحظات الاستبيان العام", "Plan — act on general survey feedback", "حلقات تحسين مستمرة", "Continuous improvement loops", "IMP-SVY-20", "imp.usr.20", "2026-04-20", "2027-04-20"),
    ];

    private static (string Ap, string Ar, string En, string DesAr, string DesEn, string Usr, string Td)[] SampleInitiativesUae() =>
    [
        ("IMP-AP-01", "ورشة «متعامل سعيد» لجميع الموظفين", "We UAE happy customer workshop", "يوم واحد — أبوظبي", "One day — Abu Dhabi", "imp.usr.01", "2026-06-20"),
        ("IMP-AP-01", "تحديث دليل التعامل مع المتعاملين", "Update customer interaction playbook", "نسخة 2026 بالعربية والإنجليزية", "2026 bilingual edition", "imp.usr.02", "2026-07-01"),
        ("IMP-AP-02", "قياس رضا أسبوعي في فرع دبي مول", "Weekly satisfaction pulse at Dubai Mall branch", "استبيان QR", "QR survey", "imp.usr.03", "2026-05-30"),
        ("IMP-AP-02", "تدريب موظفي الاستقبال على لغة الإشارة", "Reception staff sign language training", "بالتعاون مع جمعية إماراتية", "With UAE association", "imp.usr.04", "2026-08-10"),
        ("IMP-AP-03", "ربط خدمتين إضافيتين بالهوية الرقمية", "Link two more services to UAE Pass", "اختبار قبول المستخدم", "UAT with users", "imp.usr.05", "2026-09-01"),
        ("IMP-AP-03", "تقليل زمن إصدار الشهادة الإلكترونية", "Reduce e-certificate issuance time", "هدف أقل من 24 ساعة", "Target under 24h", "imp.usr.06", "2026-10-15"),
        ("IMP-AP-04", "منصة تغذية راجعة فورية في اليوم الوطني", "Instant feedback kiosks on National Day", "10 نقاط في الدولة", "10 venues nationwide", "", "2026-11-30"),
        ("IMP-AP-05", "جلسات إرشاد مهني لخريجي الجامعات", "Career coaching for new graduates", "20 جلسة ربع سنوية", "20 sessions per quarter", "imp.usr.07", "2026-07-25"),
        ("IMP-AP-06", "بوابة شفافية للشراكات على الإنترنت", "Online partnership transparency portal", "مؤشرات أداء شهرية", "Monthly KPIs", "imp.usr.08", "2026-08-05"),
        ("IMP-AP-07", "برنامج لغة إنجليزية لموظفي الاتصال", "English upskilling for contact staff", "مستوى B2 مستهدف", "Target B2 level", "imp.usr.09", "2026-12-01"),
        ("IMP-AP-08", "يوم رياضي للفرق الحكومية", "Gov teams sports day", "دعم رفاهية الموظفين", "Staff wellbeing", "imp.usr.10", "2026-06-12"),
        ("IMP-AP-09", "حملة «أطفئ الأنوار» في المبنى الرئيسي", "Switch-off lights campaign HQ", "توفير طاقة 5٪", "5% energy save", "imp.usr.11", "2026-09-22"),
        ("IMP-AP-10", "لوحة متابعة للموردين الممتازين", "Excellent vendor scoreboard", "تقدير ربع سنوي", "Quarterly recognition", "imp.usr.12", "2026-07-07"),
        ("IMP-AP-11", "سياسة استخدام داخلية للذكاء الاصطناعي", "Internal responsible AI policy", "اعتماد من الإدارة العليا", "Executive approval", "imp.usr.13", "2026-08-18"),
        ("IMP-AP-12", "تدقيق وصول للمبنى الاستراتيجي", "Accessibility audit main building", "تقرير مع توصيات", "Report with actions", "imp.usr.14", "2026-06-28"),
        ("IMP-AP-13", "مكتب إرشاد للمشاريع الصغيرة في الفرع", "SME guidance desk at branch", "يومان في الأسبوع", "Two days per week", "imp.usr.15", "2026-07-14"),
        ("IMP-AP-14", "محاكاة حريق نصف سنوية", "Bi-annual fire drill simulation", "مشاركة الدفاع المدني", "Civil defence involvement", "imp.usr.16", "2026-11-05"),
        ("IMP-AP-15", "إعادة تصميم شاشة رفع المرفقات", "Redesign attachment upload screen", "اختبار A/B", "A/B test", "imp.usr.17", "2026-09-09"),
        ("IMP-AP-16", "لقاء ربع سنوي مع المجلس المجتمعي", "Quarterly community board meeting", "محضر علني", "Public minutes", "imp.usr.18", "2026-08-28"),
        ("IMP-AP-17", "تقرير فيديو شهري من مواقع المشاريع", "Monthly video report from project sites", "على البوابة الداخلية", "On intranet", "imp.usr.19", "2026-07-19"),
    ];
}
