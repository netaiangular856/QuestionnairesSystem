using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi;
using QuestPDF.Infrastructure;
using QuestionnairesSystem.Api.Authorization;
using QuestionnairesSystem.Api.Services;
using QuestionnairesSystem.Api.Middleware;
using QuestionnairesSystem.Application.DependencyInjection;
using QuestionnairesSystem.Application.Features.Identity;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;
using QuestionnairesSystem.Infrastructure.DependencyInjection;
using QuestionnairesSystem.Persistence.DependencyInjection;
using QuestionnairesSystem.Shared.Api;
using QuestionnairesSystem.Shared.Identity;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
var trimmedOrigins = corsOrigins
    .Where(static o => !string.IsNullOrWhiteSpace(o))
    .Select(static o => o!.Trim())
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
if (trimmedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(trimmedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddHostedService<SurveyScheduleHostedService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(FormLookupAuthorizationPolicies.FormLookups, policy =>
        policy.RequireAssertion(context =>
        {
            var user = context.User;
            return user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.LookupView)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.ReportView)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.UserManage)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.RecommendationView)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.RecommendationManage)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.ActionPlanView)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.ActionPlanManage)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.SurveyView)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.SurveyManage)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.TemplateView)
                || user.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.TemplateManage);
        }));

    options.AddPolicy(AuthorizationPolicies.SurveyApproveOrManage, policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.SurveyApprove)
            || context.User.HasClaim(QuestionnairesClaimTypes.Permission, PermissionCodes.SurveyManage)));
});

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddControllers(options =>
{
    var defaultAuthPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(defaultAuthPolicy));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("default", new()
    {
        Title = "Questionnaires OS API",
        Version = "1.0",
        Description = "Enterprise Questionnaires Management System foundation API."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

var app = builder.Build();

if (app.Configuration.GetValue("IdentitySeed:RunOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var scoped = scope.ServiceProvider;
    await scoped.GetRequiredService<IIdentityDatabaseSeeder>().SeedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/default/swagger.json", "Questionnaires OS API"); });
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

if (trimmedOrigins.Length > 0)
{
    app.UseCors();
}
app.UseAuthentication();
app.UseMiddleware<AuditTrailMiddleware>();
app.UseAuthorization();

app.MapControllers();

var webRoot = app.Environment.WebRootPath;
var indexHtmlPath = string.IsNullOrWhiteSpace(webRoot) ? null : Path.Combine(webRoot, "index.html");
app.MapFallback(async (HttpContext ctx) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
    {
        var traceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        var payload = ApiResponse<object>.FromFailure(new[] { "The requested API endpoint was not found." }, traceId);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        await ctx.Response.WriteAsJsonAsync(payload, jsonOptions);
        return;
    }

    if (indexHtmlPath is not null && File.Exists(indexHtmlPath))
    {
        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.SendFileAsync(indexHtmlPath);
        return;
    }

    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
});

app.Run();

public partial class Program;
