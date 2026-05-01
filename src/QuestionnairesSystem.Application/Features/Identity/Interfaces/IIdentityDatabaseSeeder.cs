namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public interface IIdentityDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

