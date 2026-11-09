using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Public;

[ApiController]
[Route("[controller]")]
public class DemoController : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok("Fine Thanks And You?");
    }
}
