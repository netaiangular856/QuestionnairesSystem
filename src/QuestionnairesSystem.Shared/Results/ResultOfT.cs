namespace QuestionnairesSystem.Shared.Results;

public sealed class Result<T>
{
    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors, string? failureCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
        FailureCode = failureCode;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public IReadOnlyList<string> Errors { get; }

    public string? FailureCode { get; }

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<string>(), null);

    public static Result<T> Fail(string error, string? failureCode = null) =>
        new(false, default, new[] { error }, failureCode);

    public static Result<T> Fail(IReadOnlyList<string> errors, string? failureCode = null) =>
        errors is { Count: > 0 }
            ? new Result<T>(false, default, errors, failureCode)
            : Fail("Unknown error.");

    public static implicit operator Result<T>(T value) => Ok(value);
}

