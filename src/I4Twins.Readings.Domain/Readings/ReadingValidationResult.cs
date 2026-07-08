namespace I4Twins.Readings.Domain.Readings;

public sealed class ReadingValidationResult
{
    private ReadingValidationResult(Reading? reading, IReadOnlyList<string> errors)
    {
        Reading = reading;
        Errors = errors;
    }

    public Reading? Reading { get; }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Reading is not null && Errors.Count == 0;

    public static ReadingValidationResult Success(Reading reading)
    {
        return new ReadingValidationResult(reading, Array.Empty<string>());
    }

    public static ReadingValidationResult Failure(IReadOnlyList<string> errors)
    {
        return new ReadingValidationResult(null, errors);
    }
}