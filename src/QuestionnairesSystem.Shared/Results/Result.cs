namespace QuestionnairesSystem.Shared.Results;

public sealed class Result
{
    private Result(bool isSuccess, IReadOnlyList<string> errors, string? failureCode)
    {
        IsSuccess = isSuccess;
        Errors = errors;
        FailureCode = failureCode;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<string> Errors { get; }

    public string? FailureCode { get; }

    public static Result Ok() => new(true, Array.Empty<string>(), null);

    public static Result Fail(string error, string? failureCode = null) =>
        new(false, new[] { error }, failureCode);

    public static Result Fail(IReadOnlyList<string> errors, string? failureCode = null) =>
        errors is { Count: > 0 } ? new Result(false, errors, failureCode) : Fail("Unknown error.");
}

