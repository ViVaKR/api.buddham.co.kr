using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Public;
using Buddham.Core.Schemas;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Public
{
    [ApiController]
    [Route("[controller]")]
    public class AppleSyscallController(
        IAppleSyscallService service
    ) : BaseController
    {
        [HttpGet]
        public async Task<ActionResult<Result<IEnumerable<AppleSyscallSchema.Response>>>> GetAllAsync()
        {
            var result = await service.GetAsync();
            return Ok(result);
        }
    }
}
