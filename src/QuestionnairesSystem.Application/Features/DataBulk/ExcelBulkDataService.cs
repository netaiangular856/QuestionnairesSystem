using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestionnairesSystem.Application.Features.Identity.DTOs;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Application.Features.Organizations.Departments.DTOs;
using QuestionnairesSystem.Application.Features.Organizations.Departments.Interfaces;
using QuestionnairesSystem.Application.Features.Organizations.Employees.DTOs;
using QuestionnairesSystem.Application.Features.Organizations.Employees.Interfaces;
using QuestionnairesSystem.Application.Features.Partners.DTOs;
using QuestionnairesSystem.Application.Features.Partners.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.ActionPlans.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Recommendations.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Surveys.Interfaces;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.DTOs;
using QuestionnairesSystem.Application.Features.Questionnaires.Templates.Interfaces;
using QuestionnairesSystem.Domain.Enums;
using QuestionnairesSystem.Domain.Responses;
using QuestionnairesSystem.Persistence;
using QuestionnairesSystem.Shared.Results;

namespace QuestionnairesSystem.Application.Features.DataBulk;

public sealed class ExcelBulkDataService : IExcelBulkDataService
{
    private readonly QuestionnairesDbContext _db;
    private readonly IDepartmentService _departmentService;
    private readonly IEmployeeService _employeeService;
    private readonly IPartnerService _partnerService;
    private readonly IUserService _userService;
    private readonly ISurveyTemplateService _surveyTemplateService;
    private readonly ISurveyService _surveyService;
    private readonly IRecommendationCrudService _recommendationService;
    private readonly IActionPlanCrudService _actionPlanService;

    public ExcelBulkDataService(
        QuestionnairesDbContext db,
        IDepartmentService departmentService,
        IEmployeeService employeeService,
        IPartnerService partnerService,
        IUserService userService,
        ISurveyTemplateService surveyTemplateService,
        ISurveyService surveyService,
        IRecommendationCrudService recommendationService,
        IActionPlanCrudService actionPlanService)
    {
        _db = db;
        _departmentService = departmentService;
        _employeeService = employeeService;
        _partnerService = partnerService;
        _userService = userService;
        _surveyTemplateService = surveyTemplateService;
        _surveyService = surveyService;
        _recommendationService = recommendationService;
        _actionPlanService = actionPlanService;
    }

    public Task<byte[]> GetTemplateAsync(ExcelTemplateScope scope, bool includeSamples, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = ExcelBulkTemplateBuilder.Build(scope, includeSamples);
        return Task.FromResult(bytes);
    }

    public async Task<Result<ExcelImportResultDto>> ImportAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return Result<ExcelImportResultDto>.Fail(
                "استخدم ملف Excel (.xlsx) الموحد. لدمج ملفات الـ zip انسخ كل ورقة إلى مصنف واحد.",
                null);
        }

        var errors = new List<ExcelImportRowErrorDto>();
        int dCount = 0, eCount = 0, pCount = 0, uCount = 0, tCount = 0, sCount = 0, rCount = 0;
        int recCount = 0, apCount = 0, initCount = 0;

        using var wb = new XLWorkbook(stream);

        var deptCodes = await LoadDepartmentCodeMapAsync(cancellationToken).ConfigureAwait(false);

        dCount += await ImportDepartmentsAsync(wb, deptCodes, errors, cancellationToken).ConfigureAwait(false);
        eCount += await ImportEmployeesAsync(wb, deptCodes, errors, cancellationToken).ConfigureAwait(false);
        pCount += await ImportPartnersAsync(wb, deptCodes, errors, cancellationToken).ConfigureAwait(false);
        uCount += await ImportUsersAsync(wb, errors, cancellationToken).ConfigureAwait(false);
        tCount += await ImportSurveyTemplatesAsync(wb, errors, cancellationToken).ConfigureAwait(false);
        sCount += await ImportSurveysAsync(wb, errors, cancellationToken).ConfigureAwait(false);
        rCount += await ImportSurveyResponsesAsync(wb, errors, cancellationToken).ConfigureAwait(false);
        recCount += await ImportRecommendationsAsync(wb, errors, cancellationToken).ConfigureAwait(false);
        var actionPlanIdByKey = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        apCount += await ImportActionPlansAsync(wb, actionPlanIdByKey, errors, cancellationToken).ConfigureAwait(false);
        initCount += await ImportInitiativesAsync(wb, actionPlanIdByKey, errors, cancellationToken).ConfigureAwait(false);

        return Result<ExcelImportResultDto>.Ok(new ExcelImportResultDto
        {
            DepartmentsImported = dCount,
            EmployeesImported = eCount,
            PartnersImported = pCount,
            UsersImported = uCount,
            TemplatesImported = tCount,
            SurveysImported = sCount,
            ResponsesImported = rCount,
            RecommendationsImported = recCount,
            ActionPlansImported = apCount,
            InitiativesImported = initCount,
            Errors = errors,
        });
    }

    private async Task<Dictionary<string, Guid>> LoadDepartmentCodeMapAsync(CancellationToken ct)
    {
        var rows = await _db.Departments.AsNoTracking()
            .Where(x => x.RecordStatus == RecordStatus.Active)
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var map = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            map[row.Code.Trim()] = row.Id;
        }

        return map;
    }

    private async Task<int> ImportDepartmentsAsync(
        XLWorkbook wb,
        Dictionary<string, Guid> deptCodes,
        List<ExcelImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetDepartments);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        if (header.Count == 0)
        {
            return 0;
        }

        var rows = ReadDataRows(ws, header);
        var pending = rows.ToList();
        var imported = 0;

        while (pending.Count > 0)
        {
            var before = pending.Count;
            var remaining = new List<(int RowNum, Dictionary<string, string> Cells)>();

            foreach (var (rowNum, cells) in pending)
            {
                var code = GetCell(cells, "Code");
                if (string.IsNullOrWhiteSpace(code))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "Code فارغ." });
                    continue;
                }

                code = code.Trim();
                if (deptCodes.ContainsKey(code))
                {
                    continue;
                }

                var parentCode = GetCell(cells, "ParentDepartmentCode");
                Guid? parentId = null;
                if (!string.IsNullOrWhiteSpace(parentCode))
                {
                    parentCode = parentCode.Trim();
                    if (!deptCodes.TryGetValue(parentCode, out var pid))
                    {
                        remaining.Add((rowNum, cells));
                        continue;
                    }

                    parentId = pid;
                }

                var nameAr = GetCell(cells, "NameAr");
                var nameEn = GetCell(cells, "NameEn");
                if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "NameAr / NameEn مطلوبان." });
                    continue;
                }

                var result = await _departmentService.CreateAsync(
                        new CreateDepartmentRequest(code, nameAr.Trim(), nameEn.Trim(), parentId), ct)
                    .ConfigureAwait(false);
                if (!result.IsSuccess || result.Value is null)
                {
                    errors.Add(new ExcelImportRowErrorDto
                    {
                        Sheet = ws.Name,
                        RowNumber = rowNum,
                        Message = string.Join("; ", result.Errors),
                    });
                    continue;
                }

                deptCodes[code] = result.Value.Id;
                imported++;
            }

            if (remaining.Count == before)
            {
                foreach (var (rowNum, _) in remaining)
                {
                    errors.Add(new ExcelImportRowErrorDto
                    {
                        Sheet = ws.Name,
                        RowNumber = rowNum,
                        Message = "القسم الأب غير موجود أو تعارض في الترتيب.",
                    });
                }

                break;
            }

            pending = remaining;
        }

        return imported;
    }

    private async Task<int> ImportEmployeesAsync(
        XLWorkbook wb,
        IReadOnlyDictionary<string, Guid> deptCodes,
        List<ExcelImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetEmployees);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        var rows = ReadDataRows(ws, header);
        var imported = 0;

        foreach (var (rowNum, cells) in rows)
        {
            var num = GetCell(cells, "EmployeeNumber");
            if (string.IsNullOrWhiteSpace(num))
            {
                continue;
            }

            var nameAr = GetCell(cells, "NameAr");
            var nameEn = GetCell(cells, "NameEn");
            if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "NameAr / NameEn مطلوبان." });
                continue;
            }

            Guid? depId = null;
            var depCode = GetCell(cells, "DepartmentCode");
            if (!string.IsNullOrWhiteSpace(depCode))
            {
                if (!deptCodes.TryGetValue(depCode.Trim(), out var id))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"DepartmentCode غير معروف: {depCode}" });
                    continue;
                }

                depId = id;
            }

            var req = new CreateEmployeeRequest(
                num.Trim(),
                nameAr.Trim(),
                nameEn.Trim(),
                NullIfEmpty(GetCell(cells, "Email")),
                NullIfEmpty(GetCell(cells, "PhoneNumber")),
                NullIfEmpty(GetCell(cells, "JobTitleAr")),
                NullIfEmpty(GetCell(cells, "JobTitleEn")),
                depId);

            var result = await _employeeService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> ImportPartnersAsync(
        XLWorkbook wb,
        IReadOnlyDictionary<string, Guid> deptCodes,
        List<ExcelImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetPartners);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        var rows = ReadDataRows(ws, header);
        var imported = 0;

        foreach (var (rowNum, cells) in rows)
        {
            var code = GetCell(cells, "Code");
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var nameAr = GetCell(cells, "NameAr");
            var nameEn = GetCell(cells, "NameEn");
            if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "NameAr / NameEn مطلوبان." });
                continue;
            }

            if (!TryParseEnum(GetCell(cells, "Type"), out PartnerType pType))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "Type غير صالح (1–99)." });
                continue;
            }

            Guid? depId = null;
            var depCode = GetCell(cells, "DepartmentCode");
            if (!string.IsNullOrWhiteSpace(depCode))
            {
                if (!deptCodes.TryGetValue(depCode.Trim(), out var id))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"DepartmentCode غير معروف: {depCode}" });
                    continue;
                }

                depId = id;
            }

            var req = new CreatePartnerRequest(
                code.Trim(),
                nameAr.Trim(),
                nameEn.Trim(),
                pType,
                NullIfEmpty(GetCell(cells, "Email")),
                NullIfEmpty(GetCell(cells, "PhoneNumber")),
                NullIfEmpty(GetCell(cells, "ContactPerson")),
                NullIfEmpty(GetCell(cells, "Address")),
                depId);

            var result = await _partnerService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> ImportUsersAsync(XLWorkbook wb, List<ExcelImportRowErrorDto> errors, CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetUsers);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        var rows = ReadDataRows(ws, header);
        var imported = 0;

        foreach (var (rowNum, cells) in rows)
        {
            var userName = GetCell(cells, "UserName");
            var email = GetCell(cells, "Email");
            var password = GetCell(cells, "Password");
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                continue;
            }

            Guid? empId = null;
            var empNum = GetCell(cells, "EmployeeNumber");
            if (!string.IsNullOrWhiteSpace(empNum))
            {
                var e = await _db.Employees.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployeeNumber == empNum.Trim(), ct)
                    .ConfigureAwait(false);
                if (e is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"EmployeeNumber غير موجود: {empNum}" });
                    continue;
                }

                empId = e.Id;
            }

            var roleIds = await ResolveRoleIdsAsync(GetCell(cells, "RoleNames"), ct).ConfigureAwait(false);
            if (roleIds is null)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "دور غير معروف في RoleNames." });
                continue;
            }

            var req = new CreateUserRequest
            {
                UserName = userName.Trim(),
                Email = email.Trim(),
                Password = password,
                NameAr = NullIfEmpty(GetCell(cells, "NameAr")),
                NameEn = NullIfEmpty(GetCell(cells, "NameEn")),
                EmployeeId = empId,
                RoleIds = roleIds,
            };

            var result = await _userService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<IReadOnlyList<Guid>?> ResolveRoleIdsAsync(string? roleNames, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(roleNames))
        {
            return Array.Empty<Guid>();
        }

        var parts = roleNames.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var all = await _db.Roles.AsNoTracking().Select(r => new { r.Id, r.NameEn }).ToListAsync(ct).ConfigureAwait(false);
        var ids = new List<Guid>();
        foreach (var p in parts)
        {
            var m = all.FirstOrDefault(r => r.NameEn.Equals(p, StringComparison.OrdinalIgnoreCase));
            if (m is null)
            {
                return null;
            }

            ids.Add(m.Id);
        }

        return ids;
    }

    private async Task<int> ImportSurveyTemplatesAsync(XLWorkbook wb, List<ExcelImportRowErrorDto> errors, CancellationToken ct)
    {
        var wsT = FindSheet(wb, ExcelBulkTemplateBuilder.SheetSurveyTemplates);
        var wsQ = FindSheet(wb, ExcelBulkTemplateBuilder.SheetSurveyTemplateQuestions);
        if (wsT is null)
        {
            return 0;
        }

        var headerT = ReadHeader(wsT);
        var templateRows = ReadDataRows(wsT, headerT);
        var questionsByKey = new Dictionary<string, List<CreateSurveyQuestionItem>>(StringComparer.OrdinalIgnoreCase);

        if (wsQ is not null)
        {
            var headerQ = ReadHeader(wsQ);
            foreach (var (rowNum, cells) in ReadDataRows(wsQ, headerQ))
            {
                var tk = GetCell(cells, "TemplateKey");
                if (string.IsNullOrWhiteSpace(tk))
                {
                    continue;
                }

                tk = tk.Trim();
                if (!questionsByKey.TryGetValue(tk, out var list))
                {
                    list = new List<CreateSurveyQuestionItem>();
                    questionsByKey[tk] = list;
                }

                if (!int.TryParse(GetCell(cells, "DisplayOrder"), out var ord))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = wsQ.Name, RowNumber = rowNum, Message = "DisplayOrder يجب أن يكون رقماً." });
                    continue;
                }

                if (!TryParseEnum(GetCell(cells, "QuestionType"), out QuestionType qt))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = wsQ.Name, RowNumber = rowNum, Message = "QuestionType غير صالح (1–9)." });
                    continue;
                }

                var titleAr = GetCell(cells, "TitleAr");
                var titleEn = GetCell(cells, "TitleEn");
                if (string.IsNullOrWhiteSpace(titleAr) || string.IsNullOrWhiteSpace(titleEn))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = wsQ.Name, RowNumber = rowNum, Message = "عناوين السؤال مطلوبة." });
                    continue;
                }

                var opt = NullIfEmpty(GetCell(cells, "OptionsJson"));
                list.Add(new CreateSurveyQuestionItem
                {
                    Type = qt,
                    TitleAr = titleAr.Trim(),
                    TitleEn = titleEn.Trim(),
                    IsRequired = ParseBool(GetCell(cells, "IsRequired")),
                    OptionsJson = opt,
                    DisplayOrder = ord,
                });
            }
        }

        var imported = 0;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rowNum, cells) in templateRows)
        {
            var templateKey = GetCell(cells, "TemplateKey");
            if (string.IsNullOrWhiteSpace(templateKey))
            {
                continue;
            }

            templateKey = templateKey.Trim();
            if (!seenKeys.Add(templateKey))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = wsT.Name, RowNumber = rowNum, Message = "TemplateKey مكرر في الملف." });
                continue;
            }

            var nameAr = GetCell(cells, "NameAr");
            var nameEn = GetCell(cells, "NameEn");
            if (string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(nameEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = wsT.Name, RowNumber = rowNum, Message = "NameAr / NameEn مطلوبان." });
                continue;
            }

            questionsByKey.TryGetValue(templateKey, out var qList);
            if (qList is null || qList.Count == 0)
            {
                errors.Add(new ExcelImportRowErrorDto
                {
                    Sheet = wsT.Name,
                    RowNumber = rowNum,
                    Message = "يجب وجود سؤال واحد على الأقل في ورقة SurveyTemplateQuestions لنفس TemplateKey.",
                });
                continue;
            }

            var req = new CreateTemplateRequest
            {
                NameAr = nameAr.Trim(),
                NameEn = nameEn.Trim(),
                DescriptionAr = NullIfEmpty(GetCell(cells, "DescriptionAr")),
                DescriptionEn = NullIfEmpty(GetCell(cells, "DescriptionEn")),
                Questions = qList,
            };

            var result = await _surveyTemplateService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = wsT.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> ImportSurveysAsync(XLWorkbook wb, List<ExcelImportRowErrorDto> errors, CancellationToken ct)
    {
        var wsS = FindSheet(wb, ExcelBulkTemplateBuilder.SheetSurveys);
        var wsQ = FindSheet(wb, ExcelBulkTemplateBuilder.SheetSurveyQuestions);
        if (wsS is null)
        {
            return 0;
        }

        var headerS = ReadHeader(wsS);
        var surveyRows = ReadDataRows(wsS, headerS);
        var questionsByKey = new Dictionary<string, List<CreateSurveyQuestionItem>>(StringComparer.OrdinalIgnoreCase);

        if (wsQ is not null)
        {
            var headerQ = ReadHeader(wsQ);
            foreach (var (rowNum, cells) in ReadDataRows(wsQ, headerQ))
            {
                var sk = GetCell(cells, "SurveyKey");
                if (string.IsNullOrWhiteSpace(sk))
                {
                    continue;
                }

                sk = sk.Trim();
                if (!questionsByKey.TryGetValue(sk, out var list))
                {
                    list = new List<CreateSurveyQuestionItem>();
                    questionsByKey[sk] = list;
                }

                if (!int.TryParse(GetCell(cells, "DisplayOrder"), out var ord))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = wsQ.Name, RowNumber = rowNum, Message = "DisplayOrder يجب أن يكون رقماً." });
                    continue;
                }

                if (!TryParseEnum(GetCell(cells, "QuestionType"), out QuestionType qt))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = wsQ.Name, RowNumber = rowNum, Message = "QuestionType غير صالح (1–9)." });
                    continue;
                }

                var titleAr = GetCell(cells, "TitleAr");
                var titleEn = GetCell(cells, "TitleEn");
                if (string.IsNullOrWhiteSpace(titleAr) || string.IsNullOrWhiteSpace(titleEn))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = wsQ.Name, RowNumber = rowNum, Message = "عناوين السؤال مطلوبة." });
                    continue;
                }

                var opt = NullIfEmpty(GetCell(cells, "OptionsJson"));
                list.Add(new CreateSurveyQuestionItem
                {
                    Type = qt,
                    TitleAr = titleAr.Trim(),
                    TitleEn = titleEn.Trim(),
                    IsRequired = ParseBool(GetCell(cells, "IsRequired")),
                    OptionsJson = opt,
                    DisplayOrder = ord,
                });
            }
        }

        var imported = 0;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenStoredCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rowNum, cells) in surveyRows)
        {
            var surveyKey = GetCell(cells, "SurveyKey");
            if (string.IsNullOrWhiteSpace(surveyKey))
            {
                continue;
            }

            surveyKey = surveyKey.Trim();
            if (!seenKeys.Add(surveyKey))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = wsS.Name, RowNumber = rowNum, Message = "SurveyKey مكرر في الملف." });
                continue;
            }

            var titleAr = GetCell(cells, "TitleAr");
            var titleEn = GetCell(cells, "TitleEn");
            if (string.IsNullOrWhiteSpace(titleAr) || string.IsNullOrWhiteSpace(titleEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = wsS.Name, RowNumber = rowNum, Message = "TitleAr / TitleEn مطلوبان." });
                continue;
            }

            var audience = SurveyAudienceScope.AllOrganizationMembers;
            var audRaw = GetCell(cells, "AudienceScope");
            if (!string.IsNullOrWhiteSpace(audRaw) && TryParseEnum(audRaw.Trim(), out SurveyAudienceScope parsed))
            {
                audience = parsed;
            }

            var publicCode = NullIfEmpty(GetCell(cells, "PublicCode"));
            var storedCode = string.IsNullOrWhiteSpace(publicCode) ? surveyKey : publicCode!.Trim();

            if (!seenStoredCodes.Add(storedCode))
            {
                errors.Add(new ExcelImportRowErrorDto
                {
                    Sheet = wsS.Name,
                    RowNumber = rowNum,
                    Message = "الرمز المحفوظ للاستبيان (PublicCode إن وُجد وإلا SurveyKey) مكرر داخل الملف.",
                });
                continue;
            }

            var codeTaken = await _db.Surveys.AsNoTracking()
                .AnyAsync(s => s.Code == storedCode, ct)
                .ConfigureAwait(false);
            if (codeTaken)
            {
                errors.Add(new ExcelImportRowErrorDto
                {
                    Sheet = wsS.Name,
                    RowNumber = rowNum,
                    Message = $"رمز الاستبيان «{storedCode}» مستخدم مسبقاً. غيّر PublicCode أو SurveyKey، أو احذف/عدّل الاستبيان الموجود.",
                });
                continue;
            }

            questionsByKey.TryGetValue(surveyKey, out var qList);

            var req = new CreateSurveyRequest
            {
                TitleAr = titleAr.Trim(),
                TitleEn = titleEn.Trim(),
                DescriptionAr = NullIfEmpty(GetCell(cells, "DescriptionAr")),
                DescriptionEn = NullIfEmpty(GetCell(cells, "DescriptionEn")),
                Code = storedCode,
                AudienceScope = audience,
                Questions = qList ?? new List<CreateSurveyQuestionItem>(),
                ShowOnPublicPortal = ParseBool(GetCell(cells, "ShowOnPublicPortal")),
            };

            var result = await _surveyService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = wsS.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> ImportSurveyResponsesAsync(XLWorkbook wb, List<ExcelImportRowErrorDto> errors, CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetSurveyResponses);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        var rows = ReadDataRows(ws, header);
        var groups = new Dictionary<(string Sk, string Un), List<(int Row, int QOrd, string Ans)>>(new TupleComparer());

        foreach (var (rowNum, cells) in rows)
        {
            var sk = GetCell(cells, "SurveyKey");
            var un = GetCell(cells, "UserName");
            if (string.IsNullOrWhiteSpace(sk) || string.IsNullOrWhiteSpace(un))
            {
                continue;
            }

            if (!int.TryParse(GetCell(cells, "QuestionDisplayOrder"), out var qOrd))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "QuestionDisplayOrder رقم." });
                continue;
            }

            var ans = GetCell(cells, "AnswerValue");
            var key = (sk.Trim(), un.Trim().ToLowerInvariant());
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<(int, int, string)>();
                groups[key] = list;
            }

            list.Add((rowNum, qOrd, ans));
        }

        var imported = 0;

        foreach (var kv in groups)
        {
            var (surveyKey, userName) = kv.Key;
            var survey = await _db.Surveys
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.Code == surveyKey && s.RecordStatus == RecordStatus.Active, ct)
                .ConfigureAwait(false);

            if (survey is null)
            {
                foreach (var (rowNum, _, _) in kv.Value)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"استبيان غير موجود (Code): {surveyKey}" });
                }

                continue;
            }

            var unLower = userName.Trim().ToLowerInvariant();
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == unLower, ct)
                .ConfigureAwait(false);

            if (user is null)
            {
                foreach (var (rowNum, _, _) in kv.Value)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"مستخدم غير موجود: {userName}" });
                }

                continue;
            }

            var existing = await _db.SurveyResponses.AsNoTracking()
                .AnyAsync(r => r.SurveyId == survey.Id && r.RespondentUserId == user.Id && r.SubmittedAtUtc != null, ct)
                .ConfigureAwait(false);

            if (existing)
            {
                foreach (var (rowNum, _, _) in kv.Value)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "يوجد رد مسجل مسبقاً لهذا المستخدم." });
                }

                continue;
            }

            var utc = DateTime.UtcNow;
            var response = new SurveyResponse
            {
                SurveyId = survey.Id,
                RespondentUserId = user.Id,
                Status = ResponseStatus.Submitted,
                StartedAtUtc = utc,
                SubmittedAtUtc = utc,
            };

            _db.SurveyResponses.Add(response);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);

            foreach (var (_, qOrd, ansText) in kv.Value)
            {
                var q = survey.Questions.FirstOrDefault(x => x.DisplayOrder == qOrd);
                if (q is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = 0, Message = $"سؤال غير موجود للترتيب {qOrd} في {surveyKey}" });
                    continue;
                }

                var vj = ExcelAnswerJsonBuilder.Build(q.Type, ansText);
                _db.QuestionAnswers.Add(new QuestionAnswer
                {
                    ResponseId = response.Id,
                    QuestionId = q.Id,
                    ValueJson = vj,
                });
            }

            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            imported++;
        }

        return imported;
    }

    private async Task<int> ImportRecommendationsAsync(
        XLWorkbook wb,
        List<ExcelImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetRecommendations);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        if (header.Count == 0)
        {
            return 0;
        }

        var rows = ReadDataRows(ws, header);
        var imported = 0;

        foreach (var (rowNum, cells) in rows)
        {
            var titleAr = GetCell(cells, "TitleAr");
            var titleEn = GetCell(cells, "TitleEn");
            if (string.IsNullOrWhiteSpace(titleAr) || string.IsNullOrWhiteSpace(titleEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "TitleAr / TitleEn مطلوبان." });
                continue;
            }

            Guid? surveyId = null;
            var surveyKey = NullIfEmpty(GetCell(cells, "SurveyKey"));
            if (surveyKey is not null)
            {
                var sid = await TryFindSurveyIdByCodeAsync(surveyKey, ct).ConfigureAwait(false);
                if (sid is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"SurveyKey غير موجود: {surveyKey}" });
                    continue;
                }

                surveyId = sid;
            }

            Guid? assignedToUserId = null;
            var userName = NullIfEmpty(GetCell(cells, "AssignedToUserName"));
            if (userName is not null)
            {
                var uid = await TryFindUserIdByNameAsync(userName, ct).ConfigureAwait(false);
                if (uid is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"AssignedToUserName غير موجود: {userName}" });
                    continue;
                }

                assignedToUserId = uid;
            }

            var priority = 0;
            var priRaw = GetCell(cells, "Priority");
            if (!string.IsNullOrWhiteSpace(priRaw) &&
                !int.TryParse(priRaw.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out priority))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "Priority رقم صحيح." });
                continue;
            }

            DateTime? dueDate = null;
            var dueRaw = GetCell(cells, "DueDateUtc");
            if (!string.IsNullOrWhiteSpace(dueRaw))
            {
                if (!TryParseDate(dueRaw, out var parsedDue))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "DueDateUtc تاريخ غير صالح." });
                    continue;
                }

                dueDate = parsedDue;
            }

            var req = new CreateRecommendationRequest
            {
                SurveyId = surveyId,
                TitleAr = titleAr.Trim(),
                TitleEn = titleEn.Trim(),
                DescriptionAr = NullIfEmpty(GetCell(cells, "DescriptionAr")),
                DescriptionEn = NullIfEmpty(GetCell(cells, "DescriptionEn")),
                Priority = priority,
                AssignedToUserId = assignedToUserId,
                DueDateUtc = dueDate,
            };

            var result = await _recommendationService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<int> ImportActionPlansAsync(
        XLWorkbook wb,
        Dictionary<string, Guid> actionPlanIdByKey,
        List<ExcelImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetActionPlans);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        if (header.Count == 0)
        {
            return 0;
        }

        var rows = ReadDataRows(ws, header);
        var imported = 0;
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (rowNum, cells) in rows)
        {
            var key = GetCell(cells, "ActionPlanKey");
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "ActionPlanKey مطلوب." });
                continue;
            }

            key = key.Trim();
            if (!seenKeys.Add(key))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "ActionPlanKey مكرر داخل الملف." });
                continue;
            }

            var titleAr = GetCell(cells, "TitleAr");
            var titleEn = GetCell(cells, "TitleEn");
            if (string.IsNullOrWhiteSpace(titleAr) || string.IsNullOrWhiteSpace(titleEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "TitleAr / TitleEn مطلوبان." });
                continue;
            }

            Guid? surveyId = null;
            var surveyKey = NullIfEmpty(GetCell(cells, "SurveyKey"));
            if (surveyKey is not null)
            {
                var sid = await TryFindSurveyIdByCodeAsync(surveyKey, ct).ConfigureAwait(false);
                if (sid is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"SurveyKey غير موجود: {surveyKey}" });
                    continue;
                }

                surveyId = sid;
            }

            Guid? ownerUserId = null;
            var userName = NullIfEmpty(GetCell(cells, "OwnerUserName"));
            if (userName is not null)
            {
                var uid = await TryFindUserIdByNameAsync(userName, ct).ConfigureAwait(false);
                if (uid is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"OwnerUserName غير موجود: {userName}" });
                    continue;
                }

                ownerUserId = uid;
            }

            DateTime? startDate = null;
            var startRaw = GetCell(cells, "StartDateUtc");
            if (!string.IsNullOrWhiteSpace(startRaw))
            {
                if (!TryParseDate(startRaw, out var parsedStart))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "StartDateUtc تاريخ غير صالح." });
                    continue;
                }

                startDate = parsedStart;
            }

            DateTime? endDate = null;
            var endRaw = GetCell(cells, "EndDateUtc");
            if (!string.IsNullOrWhiteSpace(endRaw))
            {
                if (!TryParseDate(endRaw, out var parsedEnd))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "EndDateUtc تاريخ غير صالح." });
                    continue;
                }

                endDate = parsedEnd;
            }

            var req = new CreateActionPlanRequest
            {
                TitleAr = titleAr.Trim(),
                TitleEn = titleEn.Trim(),
                DescriptionAr = NullIfEmpty(GetCell(cells, "DescriptionAr")),
                DescriptionEn = NullIfEmpty(GetCell(cells, "DescriptionEn")),
                SurveyId = surveyId,
                OwnerUserId = ownerUserId,
                StartDateUtc = startDate,
                EndDateUtc = endDate,
            };

            var result = await _actionPlanService.CreateAsync(req, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            actionPlanIdByKey[key] = result.Value.Id;
            imported++;
        }

        return imported;
    }

    private async Task<int> ImportInitiativesAsync(
        XLWorkbook wb,
        IReadOnlyDictionary<string, Guid> actionPlanIdByKey,
        List<ExcelImportRowErrorDto> errors,
        CancellationToken ct)
    {
        var ws = FindSheet(wb, ExcelBulkTemplateBuilder.SheetInitiatives);
        if (ws is null)
        {
            return 0;
        }

        var header = ReadHeader(ws);
        if (header.Count == 0)
        {
            return 0;
        }

        var rows = ReadDataRows(ws, header);
        var imported = 0;

        foreach (var (rowNum, cells) in rows)
        {
            var apKey = GetCell(cells, "ActionPlanKey");
            if (string.IsNullOrWhiteSpace(apKey))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "ActionPlanKey مطلوب." });
                continue;
            }

            apKey = apKey.Trim();
            if (!actionPlanIdByKey.TryGetValue(apKey, out var actionPlanId))
            {
                errors.Add(new ExcelImportRowErrorDto
                {
                    Sheet = ws.Name,
                    RowNumber = rowNum,
                    Message = $"ActionPlanKey غير موجود في ورقة ActionPlans: {apKey}",
                });
                continue;
            }

            var titleAr = GetCell(cells, "TitleAr");
            var titleEn = GetCell(cells, "TitleEn");
            if (string.IsNullOrWhiteSpace(titleAr) || string.IsNullOrWhiteSpace(titleEn))
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "TitleAr / TitleEn مطلوبان." });
                continue;
            }

            Guid? ownerUserId = null;
            var userName = NullIfEmpty(GetCell(cells, "OwnerUserName"));
            if (userName is not null)
            {
                var uid = await TryFindUserIdByNameAsync(userName, ct).ConfigureAwait(false);
                if (uid is null)
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = $"OwnerUserName غير موجود: {userName}" });
                    continue;
                }

                ownerUserId = uid;
            }

            DateTime? targetDate = null;
            var tdRaw = GetCell(cells, "TargetDateUtc");
            if (!string.IsNullOrWhiteSpace(tdRaw))
            {
                if (!TryParseDate(tdRaw, out var parsedTd))
                {
                    errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = "TargetDateUtc تاريخ غير صالح." });
                    continue;
                }

                targetDate = parsedTd;
            }

            var req = new CreateInitiativeRequest
            {
                TitleAr = titleAr.Trim(),
                TitleEn = titleEn.Trim(),
                DescriptionAr = NullIfEmpty(GetCell(cells, "DescriptionAr")),
                DescriptionEn = NullIfEmpty(GetCell(cells, "DescriptionEn")),
                OwnerUserId = ownerUserId,
                TargetDateUtc = targetDate,
            };

            var result = await _actionPlanService.AddInitiativeAsync(actionPlanId, req, ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                errors.Add(new ExcelImportRowErrorDto { Sheet = ws.Name, RowNumber = rowNum, Message = string.Join("; ", result.Errors) });
                continue;
            }

            imported++;
        }

        return imported;
    }

    private async Task<Guid?> TryFindSurveyIdByCodeAsync(string code, CancellationToken ct)
    {
        var trimmed = code.Trim();
        var s = await _db.Surveys.AsNoTracking()
            .Where(x => x.Code == trimmed && x.RecordStatus == RecordStatus.Active)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return s;
    }

    private async Task<Guid?> TryFindUserIdByNameAsync(string userName, CancellationToken ct)
    {
        var n = userName.Trim().ToLowerInvariant();
        var u = await _db.Users.AsNoTracking()
            .Where(x => x.UserName == n)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return u;
    }

    private static bool TryParseDate(string raw, out DateTime value)
    {
        value = default;
        var t = raw?.Trim();
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        if (DateTime.TryParse(t, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private sealed class TupleComparer : IEqualityComparer<(string Sk, string Un)>
    {
        public bool Equals((string Sk, string Un) x, (string Sk, string Un) y) =>
            string.Equals(x.Sk, y.Sk, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Un, y.Un, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string Sk, string Un) obj) =>
            HashCode.Combine(obj.Sk.ToUpperInvariant(), obj.Un.ToUpperInvariant());
    }

    private static bool TryParseEnum<TEnum>(string raw, out TEnum value)
        where TEnum : struct, Enum
    {
        value = default;
        var t = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(t))
        {
            return false;
        }

        if (Enum.TryParse(t, true, out value))
        {
            return true;
        }

        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) &&
            Enum.IsDefined(typeof(TEnum), n))
        {
            value = (TEnum)Enum.ToObject(typeof(TEnum), n);
            return true;
        }

        return false;
    }

    private static IXLWorksheet? FindSheet(XLWorkbook wb, string name) =>
        wb.Worksheets.FirstOrDefault(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase));

    private static Dictionary<string, int> ReadHeader(IXLWorksheet ws)
    {
        var row = ws.Row(1);
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in row.CellsUsed())
        {
            var h = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(h))
            {
                map[h] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    private static List<(int RowNum, Dictionary<string, string> Cells)> ReadDataRows(IXLWorksheet ws, IReadOnlyDictionary<string, int> header)
    {
        var list = new List<(int, Dictionary<string, string>)>();
        var last = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= last; r++)
        {
            var row = ws.Row(r);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var any = false;
            foreach (var kv in header)
            {
                var v = row.Cell(kv.Value).GetString().Trim();
                dict[kv.Key] = v;
                if (!string.IsNullOrWhiteSpace(v))
                {
                    any = true;
                }
            }

            if (any)
            {
                list.Add((r, dict));
            }
        }

        return list;
    }

    private static string GetCell(IReadOnlyDictionary<string, string> cells, string key) =>
        cells.TryGetValue(key, out var v) ? v : string.Empty;

    private static string? NullIfEmpty(string? s)
    {
        var t = s?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }

    private static bool ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var s = raw.Trim();
        if (bool.TryParse(s, out var b))
        {
            return b;
        }

        if (s.Equals("1", StringComparison.Ordinal) || s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
