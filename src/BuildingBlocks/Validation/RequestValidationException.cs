namespace BuildingBlocks.Validation;

public sealed class RequestValidationException(IReadOnlyList<RequestValidationError> errors)
    : Exception("Request validation failed.")
{
    public IReadOnlyList<RequestValidationError> Errors { get; } = errors;
}
