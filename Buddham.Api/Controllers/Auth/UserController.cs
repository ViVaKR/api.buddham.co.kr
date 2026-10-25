using Buddham.Core.Common.Queries;
using Buddham.Core.Common.Results;
using Buddham.Core.Interfaces.Auth;
using Buddham.Core.Schemas;
using Buddham.Shared.Extensions;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Auth
{
    [ApiController]
    [Route("[controller]")]
    public class UserController(
        IUserService userService,
        ILogger<UserController> logger) : BaseController
    {
        /// <summary>
        /// GET LIST ALL
        /// --> User/GetListALL
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetListALL")]
        public async Task<ActionResult<Result<IEnumerable<UserSchema.Response>>>> GetAllAsync()
        {
            var result = await userService.GetAsync();
            return result.ToActionResult(this);
        }

        /// <summary>
        /// GET Cursor List
        /// --> User/GetCursorPagedList
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetCursorPagedList")]
        public async Task<ActionResult<CursorPagedResult<UserSchema.Response>>> GetCursorPagedList(
            [FromQuery] CursorPagedQuery query,
            CancellationToken ct)
        {
            var result = await userService.GetCursorListAsync(query, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// GET Offset List
        /// --> User/GetOffsetPagedList
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetOffsetPagedList")]
        public async Task<ActionResult<OffsetPagedResult<UserSchema.Response>>> GetOfsetPagedList([FromQuery] OffsetPagedQuery
            query, CancellationToken ct)
        {
            var result = await userService.GetOffsetListAsync(query, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// GET User Info By Admin
        /// </summary>
        [Authorize]
        [HttpGet("Read/{id}")]
        public async Task<ActionResult<UserSchema.Response>> ReadById(
            string id)
        {
            var result = await userService.GetUserInfoByAdminAsync(id);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Get User Info By Me
        /// </summary>
        [Authorize]
        [HttpGet("ReadInfo")]
        public async Task<ActionResult<UserSchema.Response>> ReadUserInfo()
        {
            logger.LogInformation("ReadInfo {UserIdString}", UserIdString);
            var result = await userService.GetUserInfoAsync(UserIdString);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// POST
        /// --> User/Create
        /// </summary>
        [HttpPost("Create")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Result>> Create(
            [FromBody] UserSchema.Request request,
            CancellationToken ct)
        {
            var result = await userService.CreateAsync(request, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// PUT, UPDATE
        /// --> User/Update
        /// </summary>
        [Authorize]
        [HttpPut("Update")]
        public async Task<ActionResult<Result>> Update([FromBody] UserSchema.Request request, CancellationToken ct)
        {
            var result = await userService.UpdateAsync(UserIdString, request, ct);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// PUT, RESTORE
        /// --> User/Restore/{id}
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("Restore/{id}")]
        public async Task<ActionResult<Result>> Restore(string id)
        {
            var result = await userService.RestoreAsync(id);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// DELETE
        /// --> User/Delete/{id}
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult<Result>> Delete(string id)
        {
            var result = await userService.DeleteAsync(id);
            return result.ToActionResult(this);
        }

        /// <summary>
        /// Get ALL (Admin)
        /// 관리목적 회원전체 목록
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetUsersForAdmin")]
        public async Task<ActionResult<Result<UserSchema.Response>>> GetUsersForAdmin(CancellationToken ct)
        {
            var result = await userService.GetUsersForAdminAsync(ct);
            return result.ToActionResult(this);
        }
    }
}