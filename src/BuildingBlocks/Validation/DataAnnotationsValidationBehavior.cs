using System.Collections;
using System.ComponentModel.DataAnnotations;
using MediatR;

namespace BuildingBlocks.Validation;

public sealed class DataAnnotationsValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var errors = new List<RequestValidationError>();
        ValidateObject(request, typeof(TRequest).Name, errors, 0, []);

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }

        return await next();
    }

    private static void ValidateObject(
        object? value,
        string path,
        ICollection<RequestValidationError> errors,
        int depth,
        HashSet<object> visited)
    {
        if (value is null || depth > 4 || IsSimpleType(value.GetType()) || !visited.Add(value))
        {
            return;
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                ValidateObject(item, $"{path}[{index}]", errors, depth + 1, visited);
                index++;
            }

            return;
        }

        var results = new List<ValidationResult>();
        var context = new ValidationContext(value);

        if (!Validator.TryValidateObject(value, context, results, validateAllProperties: true))
        {
            foreach (var result in results)
            {
                var members = result.MemberNames.Any() ? result.MemberNames : [string.Empty];
                foreach (var member in members)
                {
                    errors.Add(new RequestValidationError(
                        string.IsNullOrWhiteSpace(member) ? path : $"{path}.{member}",
                        result.ErrorMessage ?? "Invalid value."));
                }
            }
        }

        foreach (var property in value.GetType().GetProperties())
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            ValidateObject(property.GetValue(value), $"{path}.{property.Name}", errors, depth + 1, visited);
        }
    }

    private static bool IsSimpleType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType.IsPrimitive
            || actualType.IsEnum
            || actualType == typeof(string)
            || actualType == typeof(decimal)
            || actualType == typeof(Guid)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateTimeOffset)
            || actualType == typeof(TimeSpan);
    }
}
