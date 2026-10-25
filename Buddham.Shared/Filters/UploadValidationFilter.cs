using Buddham.Core.EnumTypes;
using Buddham.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Buddham.Shared.Filters;

public class UploadValidationFilter(UploadType type) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!UploadPolicies.Rules.TryGetValue(type, out var rule))
        {
            context.Result = new BadRequestObjectResult("Invalid upload type");
            return;
        }

        var request = context.HttpContext.Request;

        if (!request.HasFormContentType)
        {
            context.Result = new BadRequestObjectResult("Invalid content type");
            return;
        }

        var form = await request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();

        if (file == null || file.Length == 0)
        {
            context.Result = new BadRequestObjectResult("File is required");
            return;
        }

        // ✅ 1. Size 검사
        if (file.Length > rule.MaxSize)
        {
            context.Result = new BadRequestObjectResult($"File size exceeds limit ({rule.MaxSize} bytes)");
            return;
        }

        // ✅ 2. Extension 검사
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!rule.Extensions.Contains(ext))
        {
            context.Result = new BadRequestObjectResult("Invalid file extension");
            return;
        }

        // ✅ 3. MIME 검사
        if (!rule.MimeTypes.Contains(file.ContentType))
        {
            context.Result = new BadRequestObjectResult("Invalid MIME type");
            return;
        }

        // ✅ 4. (선택) Magic Number 검사
        if (!await ValidateFileSignature(file, ext))
        {
            context.Result = new BadRequestObjectResult("Invalid file signature");
            return;
        }

        await next();
    }

    private static async Task<bool> ValidateFileSignature(IFormFile file, string ext)
    {
        // ✅ using 제거 — 닫으면 안 됨!
        var stream = file.OpenReadStream();
        var buffer = new byte[8];
        _ = await stream.ReadAsync(buffer);

        // ✅ Position 리셋 — 다음 사용자를 위해
        if (stream.CanSeek) stream.Position = 0;
        Console.WriteLine(string.Join(" ", buffer.Select(b => b.ToString("X2"))));
        return ext switch
        {
            ".jpg" or ".jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8,
            ".png" => buffer[0] == 0x89 && buffer[1] == 0x50,
            ".pdf" => buffer[0] == 0x25 && buffer[1] == 0x50,
            ".mp4" => buffer[4] == 0x66 && buffer[5] == 0x74,
            ".webp" => buffer[0] == 0x52 && buffer[1] == 0x49, // ← RIFF 헤더 추가!
            _ => true
        };
    }
}