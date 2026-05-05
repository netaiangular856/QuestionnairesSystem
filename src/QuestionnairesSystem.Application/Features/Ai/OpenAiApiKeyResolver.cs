using System.Collections;
using Microsoft.Extensions.Configuration;

namespace QuestionnairesSystem.Application.Features.Ai;

/// <summary>
/// Resolves the OpenAI API key from options, environment (several names / targets), and configuration.
/// Hosting panels often store secrets only as env vars; Windows user-level vars may not be in the process block until restart.
/// </summary>
public static class OpenAiApiKeyResolver
{
    private static readonly string[] PreferredEnvNames =
    [
        "OPENAI_API_KEY_Q_AI",
        "OPENAI_API_KEY",
        "OpenAI__ApiKey",
    ];

    /// <summary>Startup-only bridge (no <see cref="IConfiguration"/> yet).</summary>
    public static string? TryResolveFromEnvironment()
    {
        foreach (var name in PreferredEnvNames)
        {
            var v = TryReadEnvAnyTarget(name);
            if (!string.IsNullOrWhiteSpace(v))
                return v.Trim();
        }

        return null;
    }

    public static string Resolve(OpenAiOptions opt, IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(opt.ApiKey))
            return opt.ApiKey.Trim();

        var fromEnv = TryResolveFromEnvironment();
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv;

        var fromConfig = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(fromConfig))
            return fromConfig.Trim();

        return string.Empty;
    }

    private static string? TryReadEnvAnyTarget(string name)
    {
        try
        {
            var v = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(v))
                return v;

            if (OperatingSystem.IsWindows())
            {
                foreach (var target in new[]
                         {
                             EnvironmentVariableTarget.Process,
                             EnvironmentVariableTarget.User,
                             EnvironmentVariableTarget.Machine,
                         })
                {
                    try
                    {
                        v = Environment.GetEnvironmentVariable(name, target);
                        if (!string.IsNullOrWhiteSpace(v))
                            return v;
                    }
                    catch
                    {
                        // User/Machine access can fail on restricted hosts; ignore.
                    }
                }
            }

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = entry.Key?.ToString();
                if (!string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                v = entry.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(v))
                    return v;
            }
        }
        catch
        {
            // Ignore — env access can fail in minimal sandboxes.
        }

        return null;
    }
}
