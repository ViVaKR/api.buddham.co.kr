using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Public;
using Buddham.Shared.Constants;
using Buddham.Shared.Extensions;
using Buddham.Shared.Filters;
using Buddham.Shared.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buddham.Api.Controllers.Public
{
    [ApiController]
    [Route("[controller]")]
    public class FileManagerController(
        IFileManagerService fileManagerService
    ) : BaseController
    {
        [Authorize]
        [HttpPost("Avatar/{id:guid?}")]
        [ValidateUpload(UploadType.Avatar)]
        [RequestSizeLimit(UploadSize.Avatar)]
        public async Task<ActionResult<Result<FileData>>> UploadAvatar(IFormFile file, Guid? id = null)
        {
            var targetuserId = id ?? UserId;
            var result = await fileManagerService.UploadAvatarAsync(
                file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                targetuserId
            );
            return result.ToActionResult(this);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("Image")]
        [ValidateUpload(UploadType.Image)]
        [RequestSizeLimit(UploadSize.Image)]
        public async Task<ActionResult<Result<FileData>>> UploadImage(IFormFile file)
            => (await fileManagerService.UploadImageAsync(
                file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                UserId)).ToActionResult(this);

        [Authorize(Roles = "Admin")]
        [HttpPost("File")]
        [ValidateUpload(UploadType.File)]
        [RequestSizeLimit(UploadSize.File)]
        public async Task<ActionResult<Result<FileData>>> UploadFile(IFormFile file)
            => (await fileManagerService.UploadFileAsync(
                file.OpenReadStream(),
                file.FileName,
                file.ContentType, UserId)).ToActionResult(this);

        [Authorize(Roles = "Admin")]
        [HttpPost("Audio")]
        [ValidateUpload(UploadType.Audio)]
        [RequestSizeLimit(UploadSize.Audio)]
        public async Task<ActionResult<Result<FileData>>> UploadAudio(IFormFile file)
            => (await fileManagerService.UploadAudioAsync(file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                UserId)).ToActionResult(this);

        [Authorize(Roles = "Admin")]
        [HttpPost("Document")]
        [ValidateUpload(UploadType.Document)]
        [RequestSizeLimit(UploadSize.Document)]
        public async Task<ActionResult<Result<FileData>>> UploadDocument(IFormFile file)
            => (await fileManagerService.UploadDocumentAsync(file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                UserId)).ToActionResult(this);

        [Authorize(Roles = "Admin")]
        [HttpPost("Video")]
        [ValidateUpload(UploadType.Video)]
        [RequestSizeLimit(UploadSize.Video)]
        public async Task<ActionResult<Result<FileData>>> UploadVideo(IFormFile file)
            => (await fileManagerService.UploadVideoAsync(file.OpenReadStream(),
                file.FileName,
                file.ContentType,
                UserId)).ToActionResult(this);
    }
}
