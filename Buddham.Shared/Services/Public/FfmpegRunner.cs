using System.Diagnostics;
using Buddham.Core.Common.Results;
using Buddham.Core.Entities.Public;
using Buddham.Core.EnumTypes;
using Buddham.Core.Interfaces.Public;
using Microsoft.Extensions.Logging;

namespace Buddham.Shared.Services.Public;

public class FfmpegRunner(ILogger<FfmpegRunner> logger) : IFfmpegRunner
{
    private const string FFMPEG = "ffmpeg";

    public async Task<FfmpegResult> RunAllAsync(VideoJob job, CancellationToken ct)
    {
        const string relativePath = "/Videos";
        string? mp4Rel = null, thumbRel = null, hlsRel = null;

        // ── A. MP4 트랜스코딩 (웹 최적화) ─────────────────────────────
        if (job.Options.TranscodeToMp4)
        {
            var mp4Out = Path.Combine(job.BasePath, $"{job.FileName}.mp4");
            var crf = job.Options.Quality switch
            {
                VideoQuality.Low => "32",
                VideoQuality.Medium => "26",
                _ => "22"
            };

            // H.264 + AAC, faststart = 웹 스트리밍 최적화
            await RunFfmpegAsync([
                "-i", job.RawPath,
                "-c:v", "libx264",
                "-crf", crf,
                "-preset", "fast",
                "-c:a", "aac",
                "-b:a", "128k",
                "-movflags", "+faststart", // ← 핵심: 메타데이터를 앞으로
                "-y",
                mp4Out
            ], ct);

            mp4Rel = $"{relativePath}/{job.FileName}.mp4";
        }

        // ── B. 썸네일 추출 ──────────────────────────────────────────────
        if (job.Options.GenerateThumbnail)
        {
            var thumbOut = Path.Combine(job.BasePath, $"{job.FileName}_thumb.jpg");
            var seekTime = job.Options.ThumbnailAtSecond.ToString();

            await RunFfmpegAsync(new[]
            {
                "-ss", seekTime, // seek 먼저 (빠름)
                "-i", job.RawPath,
                "-vframes", "1",
                "-q:v", "2", // JPEG 품질 (2=최고)
                "-vf", "scale=640:-1", // 가로 640px 유지 비율
                "-y",
                thumbOut
            }, ct);

            thumbRel = $"{relativePath}/{job.FileName}_thumb.jpg";
        }

        // ── C. HLS 세그먼트 생성 (스트리밍) ────────────────────────────
        if (job.Options.TranscodeToHls)
        {
            var hlsDir = Path.Combine(job.BasePath, $"{job.FileName}_hls");
            Directory.CreateDirectory(hlsDir);

            var m3u8Out = Path.Combine(hlsDir, "index.m3u8");
            var segPat = Path.Combine(hlsDir, "seg%03d.ts");

            await RunFfmpegAsync(new[]
            {
                "-i", job.RawPath,
                "-c:v", "libx264",
                "-crf", "24",
                "-preset", "fast",
                "-c:a", "aac",
                "-b:a", "128k",
                "-hls_time", "6", // 6초 세그먼트
                "-hls_list_size", "0", // 전체 세그먼트 목록 유지
                "-hls_segment_filename", segPat,
                "-f", "hls",
                "-y",
                m3u8Out
            }, ct);

            hlsRel = $"{relativePath}/{job.FileName}_hls/index.m3u8";
        }

        // ── D. 원본 삭제 (공간 절약) ────────────────────────────────────
        if (File.Exists(job.RawPath))
            File.Delete(job.RawPath);

        logger.LogInformation("FFmpeg completed for: {FileName}", job.FileName);

        return new FfmpegResult
        {
            Mp4RelPath = mp4Rel,
            ThumbnailRelPath = thumbRel,
            HlsRelPath = hlsRel
        };
    }

    private async Task RunFfmpegAsync(string[] args, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = FFMPEG,
            Arguments = BuildArgs(args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();

        // stderr를 비워줘야 데드락 방지
        var stderr = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            logger.LogError("FFmpeg failed: {Stderr}", stderr);
            throw new InvalidOperationException($"FFmpeg exit code {process.ExitCode}");
        }
    }

    private static string BuildArgs(string[] args)
        => string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
}
