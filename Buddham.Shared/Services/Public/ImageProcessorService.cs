using Buddham.Core.Interfaces.Public;
using SkiaSharp;

namespace Buddham.Shared.Services.Public;

public class ImageProcessorService : IImageProcessorService
{
    public async Task ProcessAsync(Stream input, string outputDir, string fileName)
    {
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        ms.Seek(0, SeekOrigin.Begin);

        using var skData = SKData.Create(ms);
        Console.WriteLine($"DEBUG: skData size = {skData?.Size}");

        using var original = SKBitmap.Decode(skData);
        Console.WriteLine($"DEBUG: decode result = {(original == null ? "NULL!" : $"{original.Width}x{original.Height}")}");

        if (original == null)
            throw new InvalidOperationException("이미지 디코딩 실패 - 지원하지 않는 포맷");

        // ✅ 원본 저장
        var originalPath = Path.Combine(outputDir, $"{fileName}_orig.jpg");
        await SaveAsJpegAsync(original, originalPath, quality: 90);

        // ✅ 표준 이미지 (1024px, 비율 유지)
        using var main = ResizeKeepAspect(original, 1024, 0);
        var mainPath = Path.Combine(outputDir, $"{fileName}.jpg");
        await SaveAsJpegAsync(main, mainPath, quality: 85);

        // ✅ 썸네일 (256x256 Crop)
        using var thumb = ResizeAndCrop(original, 256, 256);
        var thumbPath = Path.Combine(outputDir, $"{fileName}_thumb.jpg");
        await SaveAsJpegAsync(thumb, thumbPath, quality: 80);

        // ✅ WebP 변환
        var webpPath = Path.Combine(outputDir, $"{fileName}.webp");
        await SaveAsWebpAsync(main, webpPath, quality: 75);
    }

    // ─────────────────────────────────────────
    // 비율 유지 리사이즈 (ImageSharp ResizeMode.Max 동일)
    // ─────────────────────────────────────────
    private static SKBitmap ResizeKeepAspect(SKBitmap src, int maxWidth, int maxHeight)
    {
        var w = src.Width;
        var h = src.Height;

        // maxHeight == 0 이면 width 기준으로만
        if (maxHeight == 0)
        {
            if (w <= maxWidth) return src.Copy();
            var ratio = (float)maxWidth / w;

            return src.Resize(new SKImageInfo((int)(w * ratio), (int)(h * ratio)), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }

        var scaleW = (float)maxWidth / w;
        var scaleH = (float)maxHeight / h;
        var scale = Math.Min(scaleW, scaleH);

        // return src.Resize(new SKImageInfo((int)(w * scale), (int)(h * scale)), SKFilterQuality.High);
        return src.Resize(new SKImageInfo((int)(w * scale), (int)(h * scale)), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
    }

    // ─────────────────────────────────────────
    // 정사각형 Crop 리사이즈 (ImageSharp ResizeMode.Crop 동일)
    // ─────────────────────────────────────────
    private static SKBitmap ResizeAndCrop(SKBitmap src, int targetW, int targetH)
    {
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        // 1) 짧은 쪽을 기준으로 비율 맞춰 리사이즈
        var scale = Math.Max((float)targetW / src.Width, (float)targetH / src.Height);
        var scaledW = (int)(src.Width * scale);
        var scaledH = (int)(src.Height * scale);

        using var scaled = src.Resize(new SKImageInfo(scaledW, scaledH), sampling);

        // 2) 중앙에서 Crop
        var cropX = (scaledW - targetW) / 2;
        var cropY = (scaledH - targetH) / 2;

        var cropRect = new SKRectI(cropX, cropY, cropX + targetW, cropY + targetH);
        using var surface = SKSurface.Create(new SKImageInfo(targetW, targetH));
        using var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        using var image = SKImage.FromBitmap(scaled);
        canvas.DrawImage(image, cropRect, new SKRect(0, 0, targetW, targetH), sampling);

        using var snapshot = surface.Snapshot();
        return SKBitmap.FromImage(snapshot);
    }

    // ─────────────────────────────────────────
    // JPEG 저장
    // ─────────────────────────────────────────
    private static async Task SaveAsJpegAsync(SKBitmap bitmap, string path, int quality = 85)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        await using var fs = File.OpenWrite(path);
        data.SaveTo(fs);
        await fs.FlushAsync();
    }

    // ─────────────────────────────────────────
    // WebP 저장
    // ─────────────────────────────────────────
    private static async Task SaveAsWebpAsync(SKBitmap bitmap, string path, int quality = 75)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Webp, quality);
        await using var fs = File.OpenWrite(path);
        data.SaveTo(fs);
        await fs.FlushAsync();
    }
}
/*
    -> ✔ 원본 저장 (선택)
    -> ✔ 표준 이미지 (예: 1024px)
    -> ✔ 썸네일 (예: 256px)
    -> ✔ WebP 변환 (선택)
    -> ✔ 비율 유지 + Crop 옵션
    -> ✔ 파일명 안전화
 */
