using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Auth;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Buddham.Shared.Extensions;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Auth
{
    [ApiController]
    [Route("[controller]")]
    public class UserRoleController(
        RoleManager<ApplicationRole> roleManager,
        IUserRoleService userRoleService
    ) : BaseController
    {
        /// <summary>
        ///     GET
        ///     UserRole/GetListALL
        /// </summary>
        [HttpGet("GetListALL")]
        public async Task<ActionResult<Result<IEnumerable<UserRoleSchema.Response>>>> GetListALL()
        {
            var result = await userRoleService.GetAsync();
            return result.ToActionResult(this);
        }

        /// <summary>
        /// GET Cursor List
        /// </summary>
        [HttpGet("GetCursorPagedList")]
        public async Task<ActionResult<CursorPagedResult<UserRoleSchema
            .Response>>> GetCursorPagedList([FromQuery] CursorPagedQuery query,
            CancellationToken ct)
        {
            var result = await userRoleService.GetCursorListAsync(query, ct);

            return result.ToActionResult(this);
        }


        /// <summary>
        ///     GET Offset List
        ///     UserRole/GetOffsetPagedList
        /// </summary>
        [HttpGet("GetOffsetPagedList")]
        public async Task<ActionResult<OffsetPagedResult<UserRoleSchema.Response>>> GetOffsetPagedList(
            [FromQuery] OffsetPagedQuery request, CancellationToken ct)
        {
            var result = await userRoleService.GetOffsetListAsync(request, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        ///     GET by id
        ///     UserRole/Read/{id}
        /// </summary>
        [HttpGet("Read/{id}")]
        public async Task<ActionResult<Result<UserRoleSchema.Request>>> ReadById(string id)
        {
            var result = await userRoleService.GetUserInfoByAdminAsync(id);
            return result.ToActionResult(this);
        }

        /// <summary>
        ///     POST
        ///     UserRole/Create
        /// </summary>
        [HttpPost("Create")]
        public async Task<ActionResult<Result>> Create([FromBody] UserRoleSchema.Request request, CancellationToken ct)
        {
            var result = await userRoleService.CreateAsync(request, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        ///     PUT
        ///     UserRole/Update/{id}
        /// </summary>
        [HttpPut("Update/{id}")]
        public async Task<ActionResult<Result>> Update(
            string id,
            [FromBody] UserRoleSchema.Request request,
            CancellationToken ct)
        {
            var result = await userRoleService.UpdateAsync(id, request, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        ///     DELETE
        ///     UserRole/Delete/{id}
        /// </summary>
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result>> Delete(string id)
        {
            var result = await userRoleService.DeleteAsync(id);
            return result.ToActionResult(this);
        }

        /// <summary>
        ///     CheckName
        ///     중복체크 (권한 영문명)
        /// </summary>
        [HttpGet("CheckName/{name}")]
        public async Task<ActionResult<bool>> CheckName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return BadRequest("잘못된 이름입니다.");

            // RoleManage 내부에서 자동으로 대소문자를 무시하고
            // 인덱스를 활용하여 검색
            var exists = await roleManager.RoleExistsAsync(name);
            return exists;
        }
    }
}