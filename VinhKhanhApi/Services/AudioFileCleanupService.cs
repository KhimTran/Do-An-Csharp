using Microsoft.EntityFrameworkCore;
using VinhKhanhApi.Data;

namespace VinhKhanhApi.Services
{
    public class AudioFileCleanupService : IAudioFileCleanupService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AudioFileCleanupService> _logger;

        public AudioFileCleanupService(
            AppDbContext db,
            IWebHostEnvironment env,
            ILogger<AudioFileCleanupService> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        public async Task DeleteAudioFileIfUnusedAsync(
            string? fileName,
            int? excludingPoiId = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedFileName = NormalizeAudioFileName(fileName);
            if (normalizedFileName == null || !IsSafeAudioFileName(normalizedFileName))
            {
                return;
            }

            var isReferenced = await _db.POIs.AnyAsync(poi =>
                (!excludingPoiId.HasValue || poi.Id != excludingPoiId.Value) &&
                (poi.TenFileAudio_Vi == normalizedFileName ||
                 poi.TenFileAudio_En == normalizedFileName ||
                 poi.TenFileAudio_Zh == normalizedFileName ||
                 poi.AudioFileViDeXuat == normalizedFileName ||
                 poi.AudioFileEnDeXuat == normalizedFileName ||
                 poi.AudioFileZhDeXuat == normalizedFileName),
                cancellationToken);

            if (isReferenced)
            {
                return;
            }

            var audioPath = GetAudioPath(normalizedFileName);
            try
            {
                if (System.IO.File.Exists(audioPath))
                {
                    System.IO.File.Delete(audioPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete unused audio file: {FileName}", normalizedFileName);
            }
        }

        private string? NormalizeAudioFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            var trimmedFileName = fileName.Trim();
            var normalizedFileName = Path.GetFileName(trimmedFileName);
            if (!string.Equals(trimmedFileName, normalizedFileName, StringComparison.Ordinal))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(normalizedFileName) ? null : normalizedFileName;
        }

        private string GetAudioPath(string fileName)
        {
            var webRootPath = string.IsNullOrWhiteSpace(_env.WebRootPath)
                ? Path.Combine(_env.ContentRootPath, "wwwroot")
                : _env.WebRootPath;
            var audioDirectory = Path.GetFullPath(Path.Combine(webRootPath, "audio"));
            var audioPath = Path.GetFullPath(Path.Combine(audioDirectory, fileName));
            var audioDirectoryPrefix = audioDirectory.EndsWith(Path.DirectorySeparatorChar)
                ? audioDirectory
                : audioDirectory + Path.DirectorySeparatorChar;

            if (!audioPath.StartsWith(audioDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Audio path is outside wwwroot/audio.");
            }

            return audioPath;
        }

        private bool IsSafeAudioFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            if (Path.IsPathRooted(fileName) || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(Path.GetExtension(fileName), ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
        }
    }
}
