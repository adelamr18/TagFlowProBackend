using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using TagFlowApi.DTOs;
using TagFlowApi.Hubs;
using TagFlowApi.Repositories;

namespace TagFlowApi.Controllers
{
    [ApiController]
    [Route("api/file")]
    public class FileController : ControllerBase
    {
        private readonly FileRepository _fileRepository;
        private readonly string _mergedFilesPath = "/var/lib/tagflow/merged";
        private readonly IHubContext<FileStatusHub> _hubContext;

        public FileController(FileRepository fileRepository, IHubContext<FileStatusHub> hubContext)
        {
            _fileRepository = fileRepository;
            _hubContext = hubContext;

            if (!Directory.Exists(_mergedFilesPath))
            {
                Directory.CreateDirectory(_mergedFilesPath);
            }
        }

        [HttpPost("upload-file")]
        public async Task<IActionResult> UploadFile(
       [FromForm] string addedFileName,
       [FromForm] string fileStatus,
       [FromForm] int fileRowsCount,
       [FromForm] string uploadedByUserName,
       [FromForm] int selectedProjectId,
       [FromForm] string? selectedPatientTypeIds,
       [FromForm] int userId,
       [FromForm] bool isAdmin,
       [FromForm] string fileUploadedOn,
       [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided." });
                }

                using var memory = new MemoryStream();
                await file.CopyToAsync(memory);
                memory.Position = 0;

                var ssnIds = await _fileRepository.ExtractSsnIdsFromExcel(memory);
                foreach (var ssn in ssnIds)
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(ssn, "^[123]\\d{9}$"))
                    {
                        return BadRequest(new { success = false, message = $"Invalid SSN: {ssn}. SSNs must be exactly 10 digits and start with 1, 2, or 3." });
                    }
                }

                // Reset memory stream position for the actual file upload
                memory.Position = 0;

                var patientTypeIds = string.IsNullOrEmpty(selectedPatientTypeIds)
                    ? new List<int>()
                    : Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(selectedPatientTypeIds);

                var addFileDto = new AddFileDto
                {
                    AddedFileName = addedFileName,
                    FileStatus = fileStatus,
                    FileRowsCount = fileRowsCount,
                    UploadedByUserName = uploadedByUserName,
                    SelectedProjectId = selectedProjectId,
                    SelectedPatientTypeIds = patientTypeIds,
                    UserId = userId,
                    IsAdmin = isAdmin,
                    File = file,
                    FileUploadedOn = string.IsNullOrWhiteSpace(fileUploadedOn)
           ? DateTime.UtcNow
           : DateTime.Parse(fileUploadedOn).ToUniversalTime()
                };

                using (var fileStream = new MemoryStream(memory.ToArray()))
                {
                    var (fileName, fileId) = await _fileRepository.UploadFileAsyncTwo(addFileDto, fileStream);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        return Ok(new
                        {
                            success = true,
                            fileName,
                            fileId,
                            message = "File uploaded successfully! You can download the merged file using the provided file name."
                        });
                    }
                    return Ok(new { success = true, message = "File uploaded successfully. You can find the downloaded file in the file status table" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("fetch-unprocessed-ssns")]
        public async Task<IActionResult> FetchUnprocessedSSNs([FromQuery] int batchSize = 50)
        {
            try
            {
                var unprocessedRows = await _fileRepository.FetchAndLockUnprocessedRowsAsync(batchSize);

                return Ok(new
                {
                    success = true,
                    data = unprocessedRows.Select(row => new
                    {
                        row.FileRowId,
                        row.SsnId,
                        row.FileId
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("update-processed-data")]
        public async Task<IActionResult> UpdateProcessedData([FromQuery] int fileId, [FromBody] List<FileRowDto> updates)
        {
            try
            {
                await _fileRepository.UpdateProcessedDataAsync(fileId, updates, _hubContext);

                return Ok(new { success = true, message = "Data updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] int fileId, [FromQuery] string? fileName = null)
        {
            var fileRec = await _fileRepository.GetFileByIdAsync(fileId);
            if (fileRec is null)
                return NotFound("Record not found.");

            string mergedDefault = $"File_{fileId}_Merged.xlsx";

            string resolvedName = !string.IsNullOrWhiteSpace(fileName)
                ? Path.GetFileName(fileName)
                : mergedDefault;

            if (resolvedName == mergedDefault && !string.IsNullOrWhiteSpace(fileRec.DownloadLink))
            {
                try
                {
                    var uri = new Uri(fileRec.DownloadLink, UriKind.RelativeOrAbsolute);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var fromLink = query.Get("fileName");
                    if (!string.IsNullOrWhiteSpace(fromLink))
                        resolvedName = Path.GetFileName(fromLink);
                }
                catch { /* ignore parse issues */ }
            }

            var path = Path.Combine(_mergedFilesPath, resolvedName);
            if (!System.IO.File.Exists(path))
            {

                var fallbackMerged = Path.Combine(_mergedFilesPath, mergedDefault);
                if (System.IO.File.Exists(fallbackMerged))
                    path = fallbackMerged;
                else
                {
                    var orig = Path.Combine(_mergedFilesPath, Path.GetFileName(fileRec.FileName ?? string.Empty));
                    if (!string.IsNullOrWhiteSpace(fileRec.FileName) && System.IO.File.Exists(orig))
                        path = orig;
                    else
                        return NotFound("File not found on disk.");
                }
            }

            await _fileRepository.UpdateFileDownloadLinkAsync(fileId, Path.GetFileName(path));

            const string xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return PhysicalFile(path, xlsx, fileDownloadName: Path.GetFileName(path));
        }


        [HttpGet("get-all-files")]
        public async Task<IActionResult> GetAllFiles()
        {
            try
            {
                var files = await _fileRepository.GetAllFilesAsync();

                return Ok(new { success = true, files });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("delete/{fileId}")]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            try
            {
                var file = await _fileRepository.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound(new { success = false, message = "File not found." });
                }

                await _fileRepository.DeleteFileAsync(fileId);

                return Ok(new { success = true, message = "File deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(
                  [FromQuery] DateTime? fromDate,
                  [FromQuery] DateTime? toDate,
                  [FromQuery] string? projectName,
                  [FromQuery] string? patientType,
                  [FromQuery] int? viewerId = null)
        {
            try
            {
                var overview = await _fileRepository.GetOverviewAsync(fromDate, toDate, projectName, patientType, viewerId);
                return Ok(new { success = true, overview });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("project-analytics")]
        public async Task<IActionResult> GetProjectAnalytics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? projectName,
        [FromQuery] string? patientType,
        [FromQuery] string? timeGranularity)
        {

            try
            {
                var analytics = await _fileRepository.GetProjectAnalyticsAsync(fromDate, toDate, projectName, patientType, timeGranularity);
                return Ok(new { success = true, analytics });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }

        }

        [HttpPost("robot-errors")]
        public async Task<IActionResult> AddRobotError([FromBody] RobotErrorCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Module) || string.IsNullOrWhiteSpace(dto.ErrorMessage))
                    return BadRequest(new { success = false, message = "Module and ErrorMessage are required." });

                var newId = await _fileRepository.AddRobotErrorAsync(dto);

                return Ok(new
                {
                    success = true,
                    id = newId,
                    message = "Robot error logged successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("debug-write")]
        public IActionResult DebugWrite([FromQuery] int fileId = 999)
        {
            try
            {
                var fn = $"debug_{fileId}.txt";
                var path = Path.Combine("/var/lib/tagflow/merged", fn);
                System.IO.File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
                var exists = System.IO.File.Exists(path);
                return Ok(new { success = exists, path });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        // Add inside the FileController class

        // FileController.cs (inside the FileController class)

        [HttpGet("diag-write")]
        [AllowAnonymous]  // so only the header check applies
        public IActionResult DiagWrite([FromServices] IConfiguration cfg, [FromServices] ILogger<FileController> logger)
        {
            var provided = Request.Headers["X-Api-Key"].FirstOrDefault();
            var expected = cfg["ApiKey"]
                           ?? Environment.GetEnvironmentVariable("API_KEY")
                           ?? Environment.GetEnvironmentVariable("ApiKey");

            if (string.IsNullOrWhiteSpace(expected) || !string.Equals(provided, expected))
            {
                logger.LogWarning("diag-write: API key missing/invalid.");
                return Unauthorized("API Key is missing or invalid.");
            }

            var dir = "/var/lib/tagflow/merged";
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            var path = System.IO.Path.Combine(dir, $"_diag_{stamp}.txt");

            try
            {
                System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(path, $"ok {stamp}\n");
                logger.LogInformation("diag-write wrote {Path}", path);
                return Ok(new { ok = true, path });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "diag-write failed writing {Path}", path);
                return StatusCode(500, new { ok = false, error = ex.Message, path });
            }
        }


        [HttpGet("where-are-files")]
        public IActionResult WhereAreFiles([FromServices] IConfiguration cfg)
        {
            var configured = cfg["MergedDir"] ?? Environment.GetEnvironmentVariable("MERGED_DIR") ?? "/var/lib/tagflow/merged";
            return Ok(new { configured });
        }




    }
}
