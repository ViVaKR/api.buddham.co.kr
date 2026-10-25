using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Buddham.Shared.Filters;

public class ValidateFilter<T>(IValidator<T> validator) : IAsyncActionFilter
    where T : class
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var arg = context.ActionArguments.Values.OfType<T>().FirstOrDefault();

        if (arg is not null)
        {
            var result = await validator.ValidateAsync(arg);
            if (!result.IsValid)
            {
                context.Result = new BadRequestObjectResult(result.ToDictionary());
                return;
            }
        }

        await next();
    }
}