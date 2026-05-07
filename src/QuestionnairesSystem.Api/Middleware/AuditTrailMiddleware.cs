using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using QuestionnairesSystem.Domain.Audit;
using QuestionnairesSystem.Persistence;

namespace QuestionnairesSystem.Api.Middleware;

/// <summary>
/// Lightweight audit trail for API calls.
/// </summary>
public sealed class AuditTrailMiddleware
{
    private readonly RequestDelegate _next;

    public AuditTrailMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Skip auth endpoints to avoid noise on login attempts.
        if (context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Only mutations: skip GET and other safe methods.
        if (!IsMutatingMethod(context.Request.Method))
        {
            return;
        }

        try
        {
            var db = context.RequestServices.GetService<QuestionnairesDbContext>();
            if (db is null)
            {
                return;
            }

            // Detach any entities that may still be tracked by the request-scoped
            // DbContext so we never accidentally flush partially-applied mutations
            // from a failed business operation when persisting the audit row.
            db.ChangeTracker.Clear();

            var userId = TryReadUserId(context.User);
            var action = context.Request.Method.ToUpperInvariant();
            var path = context.Request.Path.Value ?? string.Empty;

            db.AuditLogs.Add(new AuditLog
            {
                OccurredAtUtc = DateTime.UtcNow,
                UserId = userId,
                Action = action,
                EntityType = path,
                EntityId = context.TraceIdentifier,
                NewValuesJson = $"{{\"statusCode\":{context.Response.StatusCode}}}",
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers.UserAgent.ToString()
            });

            await db.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
        }
        catch
        {
            // Never fail the request when audit logging fails.
        }
    }

    private static Guid? TryReadUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue("uid");

        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static bool IsMutatingMethod(string method) =>
        HttpMethods.IsPost(method)
        || HttpMethods.IsPut(method)
        || HttpMethods.IsPatch(method)
        || HttpMethods.IsDelete(method);
}
