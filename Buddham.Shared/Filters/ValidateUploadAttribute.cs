using Buddham.Core.EnumTypes;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Shared.Filters;

public class ValidateUploadAttribute : TypeFilterAttribute
{
    public ValidateUploadAttribute(UploadType type) : base(typeof(UploadValidationFilter))
    {
        Arguments = [type];
    }
}