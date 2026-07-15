using System.Diagnostics.CodeAnalysis;

namespace VuzScrapper.Scrappers.Common;

internal sealed record Result<TValue, TError>
{
    public TValue? Value { get; set; }
    public TError? Error { get; set; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    [MemberNotNullWhen(true, nameof(Error))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFailure => !IsSuccess;

    private Result(TValue value)
    {
        Value = value;
        Error = default;
    }

    private Result(TError error)
    {
        Value = default;
        Error = error;
    }

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);
}