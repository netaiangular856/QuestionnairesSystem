using Microsoft.AspNetCore.Hosting;
using QuestionnairesSystem.Application.Features.Identity.Interfaces;

namespace QuestionnairesSystem.Infrastructure.Identity;

public sealed class WebRootAvatarStorage : IAvatarStorage
{
    private const long MaxBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly string _avatarsDirectory;

    public WebRootAvatarStorage(IWebHostEnvironment env)
    {
        var root = string.IsNullOrWhiteSpace(env.WebRootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot")
            : env.WebRootPath;
        _avatarsDirectory = Path.Combine(root, "uploads", "avatars");
    }

    public async Task<string> SaveAsync(
        Guid userId,
        Stream content,
        string extensionWithDot,
        CancellationToken cancellationToken = default)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var ext = NormalizeExtension(extensionWithDot);
        if (ext is null || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException("Unsupported image type.");
        }

        Directory.CreateDirectory(_avatarsDirectory);
        TryDeleteAllForUser(userId);

        var fileName = $"{userId:N}{ext}";
        var fullPath = Path.Combine(_avatarsDirectory, fileName);

        await using var target = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        var buffer = new byte[81920];
        long written = 0;
        int read;
        while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
        {
            written += read;
            if (written > MaxBytes)
            {
                target.Close();
                TryDelete(fileName);
                throw new InvalidOperationException("File is too large.");
            }

            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return fileName;
    }

    public void TryDelete(string? storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName))
        {
            return;
        }

        var safe = Path.GetFileName(storedFileName.Trim());
        if (string.IsNullOrEmpty(safe) || !string.Equals(safe, storedFileName.Trim(), StringComparison.Ordinal))
        {
            return;
        }

        var path = Path.Combine(_avatarsDirectory, safe);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort
        }
    }

    public void TryDeleteAllForUser(Guid userId)
    {
        if (!Directory.Exists(_avatarsDirectory))
        {
            return;
        }

        var prefix = $"{userId:N}.";
        try
        {
            foreach (var path in Directory.GetFiles(_avatarsDirectory))
            {
                var name = Path.GetFileName(path);
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch
                    {
                        // best effort
                    }
                }
            }
        }
        catch
        {
            // best effort
        }
    }

    private static string? NormalizeExtension(string extensionWithDot)
    {
        if (string.IsNullOrWhiteSpace(extensionWithDot))
        {
            return null;
        }

        var t = extensionWithDot.Trim();
        if (!t.StartsWith(".", StringComparison.Ordinal))
        {
            t = "." + t;
        }

        return t.ToLowerInvariant();
    }
}
