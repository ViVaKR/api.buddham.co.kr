using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Buddham.Shared.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class ValidateRequestAttribute<T> : Attribute, IFilterFactory
    where T : class
{
    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        => new ValidateFilter<T>(serviceProvider.GetRequiredService<IValidator<T>>());
}