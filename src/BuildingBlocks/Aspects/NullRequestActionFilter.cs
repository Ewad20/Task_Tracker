using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BuildingBlocks.Aspects;

public sealed class NullRequestActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var parameter in context.ActionDescriptor.Parameters.OfType<ControllerParameterDescriptor>())
        {
            if (IsBodyLikeParameter(parameter.ParameterInfo.ParameterType)
                && context.ActionArguments.TryGetValue(parameter.Name, out var value)
                && value is null)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    error = $"Request parameter '{parameter.Name}' cannot be null.",
                    traceId = context.HttpContext.TraceIdentifier
                });
                return;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static bool IsBodyLikeParameter(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType != typeof(string)
            && actualType != typeof(Guid)
            && actualType != typeof(DateTime)
            && actualType != typeof(DateTimeOffset)
            && actualType != typeof(CancellationToken)
            && !actualType.IsPrimitive
            && !actualType.IsEnum;
    }
}
