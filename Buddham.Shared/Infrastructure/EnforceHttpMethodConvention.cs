using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Infrastructure;

public class EnforceHttpMethodConvention(
    ILogger<EnforceHttpMethodConvention> logger,
    bool throwOnMissingMethod = false
) : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.ControllerName == "Error") continue;

            foreach (var action in controller.Actions)
            {
                // [NonAction] 은 스킵
                if (action.Attributes.Any(x => x is NonActionAttribute)) continue;

                // HttpMethodAttribute 여부 체크
                var hasHttpMethod = action.Attributes.OfType<HttpMethodAttribute>().Any();

                if (hasHttpMethod) continue;

                var message = $"⚠️ Missing HTTP method: {controller.ControllerName}.{action.ActionName} (Route: {string.Join(", ", action.Selectors.Select(s => s.AttributeRouteModel?.Template ?? "(none)"))})";

                if (throwOnMissingMethod)
                    throw new InvalidOperationException($"❌ API Method Missing Http Method Attribute: {message}");

                logger.LogWarning(message);
            }
        }
    }
}
