namespace BuildingBlocks.Validation;

public sealed record RequestValidationError(string Field, string Message);
