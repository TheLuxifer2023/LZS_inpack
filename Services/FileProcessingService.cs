using System.IO.Compression;
using Microsoft.AspNetCore.Components.Forms;

namespace LZS_Web.Services
{
    public class FileProcessingService
    {
        private readonly ILogger<FileProcessingService> _logger;
        private readonly string _uploadsPath;
        private readonly string _downloadsPath;

        public FileProcessingService(ILogger<FileProcessingService> logger)
        {
            _logger = logger;
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            _downloadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "downloads");
            
            Directory.CreateDirectory(_uploadsPath);
            Directory.CreateDirectory(_downloadsPath);
        }

        public async Task<string> SaveUploadedFileAsync(IFormFile file, string? customName = null)
        {
            var fileName = customName ?? $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(_uploadsPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation($"File saved: {fileName}");
            return filePath;
        }

        public async Task<string> SaveBrowserFileAsync(IBrowserFile file, string? customName = null)
        {
            var fileName = customName ?? $"{Guid.NewGuid()}_{file.Name}";
            var filePath = Path.Combine(_uploadsPath, fileName);

            using var stream = file.OpenReadStream(maxAllowedSize: 100_000_000);
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);

            _logger.LogInformation($"File saved: {fileName}");
            return filePath;
        }

        public async Task<string> SaveDownloadFileAsync(string sourcePath, string fileName)
        {
            var downloadPath = Path.Combine(_downloadsPath, fileName);
            File.Copy(sourcePath, downloadPath, true);
            
            _logger.LogInformation($"Download file created: {fileName}");
            return $"/downloads/{fileName}";
        }

        public string GetFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public string GetFileExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLower();
        }

        public bool IsValidPhyreFile(string fileName)
        {
            var ext = GetFileExtension(fileName);
            return ext == ".phyre";
        }

        public bool IsValidFontFile(string fileName)
        {
            var ext = GetFileExtension(fileName);
            return ext == ".fnt";
        }

        public bool IsValidTextureFile(string fileName)
        {
            var ext = GetFileExtension(fileName);
            return ext == ".dds" || ext == ".gtf" || ext == ".png";
        }

        public void CleanupOldFiles()
        {
            try
            {
                // Clean files older than 1 hour
                var cutoff = DateTime.Now.AddHours(-1);
                
                CleanupDirectory(_uploadsPath, cutoff);
                CleanupDirectory(_downloadsPath, cutoff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old files");
            }
        }

        private void CleanupDirectory(string directory, DateTime cutoff)
        {
            var files = Directory.GetFiles(directory);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogInformation($"Deleted old file: {fileInfo.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Could not delete file: {fileInfo.Name}");
                    }
                }
            }
        }

        public List<FileInfo> GetResultFiles(string baseName)
        {
            var files = new List<FileInfo>();
            var searchPatterns = new[]
            {
                $"{baseName}*.fnt",
                $"{baseName}*.json", 
                $"{baseName}*.dds",
                $"{baseName}*.gtf",
                $"{baseName}*.png",
                $"{baseName}*.phyre"
            };

            foreach (var pattern in searchPatterns)
            {
                files.AddRange(Directory.GetFiles(_uploadsPath, pattern).Select(f => new FileInfo(f)));
            }

            return files.OrderBy(f => f.CreationTime).ToList();
        }
    }
}
