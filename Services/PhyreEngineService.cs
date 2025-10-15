using System.Diagnostics;
using LZS_unpack;

namespace LZS_Web.Services
{
    public class PhyreEngineService
    {
        private readonly ILogger<PhyreEngineService> _logger;
        private readonly string _executablePath;

        public PhyreEngineService(ILogger<PhyreEngineService> logger)
        {
            _logger = logger;
            _executablePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LZS_inpack.exe");
        }

        public async Task<ProcessResult> AnalyzeFileAsync(string filePath)
        {
            return await RunCommandAsync("-analyze", filePath);
        }

        public async Task<ProcessResult> ExtractTextureAsync(string filePath)
        {
            return await RunCommandAsync("-texture", filePath);
        }

        public async Task<ProcessResult> ExtractCharactersAsync(string filePath, int offset, int count, int size)
        {
            return await RunCommandAsync("-extractchar", filePath, offset.ToString(), count.ToString(), size.ToString());
        }

        public async Task<ProcessResult> PackFontAsync(string fntPath, string texturePath, string outputPath)
        {
            return await RunCommandAsync("-packfont", fntPath, texturePath, outputPath);
        }

        public async Task<ProcessResult> VerifyPackedFileAsync(string packedPath, string? originalPath = null)
        {
            var args = new List<string> { "-verify", packedPath };
            if (!string.IsNullOrEmpty(originalPath))
                args.Add(originalPath);
            
            return await RunCommandAsync(args.ToArray());
        }

        public async Task<ProcessResult> DetectFormatAsync(string filePath)
        {
            return await RunCommandAsync("-detect", filePath);
        }

        public async Task<ProcessResult> ConvertToPNGAsync(string inputPath, string outputPath)
        {
            return await RunCommandAsync("-topng", inputPath, outputPath);
        }

        public async Task<ProcessResult> ConvertToDDSAsync(string inputPath, string outputPath)
        {
            return await RunCommandAsync("-todds", inputPath, outputPath);
        }

        public async Task<ProcessResult> FindFontDataAsync(string filePath)
        {
            return await RunCommandAsync("-finddata", filePath);
        }

        public async Task<ProcessResult> FindStructSizeAsync(string filePath, int offset)
        {
            return await RunCommandAsync("-findsize", filePath, offset.ToString());
        }

        private async Task<ProcessResult> RunCommandAsync(params string[] args)
        {
            try
            {
                _logger.LogInformation($"Running command: {_executablePath} {string.Join(" ", args)}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = _executablePath,
                    Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                return new ProcessResult
                {
                    Success = process.ExitCode == 0,
                    Output = output,
                    Error = error,
                    ExitCode = process.ExitCode
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running command");
                return new ProcessResult
                {
                    Success = false,
                    Output = "",
                    Error = ex.Message,
                    ExitCode = -1
                };
            }
        }
    }

    public class ProcessResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }
    }
}
