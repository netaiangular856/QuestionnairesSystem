namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public interface IAvatarStorage
{
    /// <summary>Save image for user; returns stored file name (e.g. guid.jpg).</summary>
    Task<string> SaveAsync(Guid userId, Stream content, string extensionWithDot, CancellationToken cancellationToken = default);

    void TryDelete(string? storedFileName);

    void TryDeleteAllForUser(Guid userId);
}
