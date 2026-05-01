namespace QuestionnairesSystem.Shared.Api;

public sealed class ApiResponse
{
    public bool Success { get; init; }

    public IReadOnlyList<string>? Errors { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse FromSuccess(string? traceId = null) =>
        new() { Success = true, TraceId = traceId };

    public static ApiResponse FromFailure(IReadOnlyList<string> errors, string? traceId = null) =>
        new() { Success = false, Errors = errors, TraceId = traceId };
}

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public T? Data { get; init; }

    public IReadOnlyList<string>? Errors { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse<T> FromSuccess(T data, string? traceId = null) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiResponse<T> FromFailure(IReadOnlyList<string> errors, string? traceId = null) =>
        new() { Success = false, Errors = errors, TraceId = traceId };
}

