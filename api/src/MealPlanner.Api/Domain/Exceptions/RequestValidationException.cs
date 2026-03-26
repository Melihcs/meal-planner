namespace MealPlanner.Api.Domain.Exceptions;

public sealed class RequestValidationException(
    IReadOnlyDictionary<string, string[]> errors,
    string message = "One or more validation errors occurred.")
    : Exception(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
