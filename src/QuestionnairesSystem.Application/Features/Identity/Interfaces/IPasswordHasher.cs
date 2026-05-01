namespace QuestionnairesSystem.Application.Features.Identity.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

