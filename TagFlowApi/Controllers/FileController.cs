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
        private readonly string _mergedFilesPath = Path.Combine(Path.GetTempPath(), "MergedFiles");
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
                    var (fileName, fileId) = await _fileRepository.UploadFileAsync(addFileDto, fileStream);
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

            var safeName = string.IsNullOrWhiteSpace(fileRec.FileName)
                ? (fileName ?? $"file_{fileId}.xlsx")
                : fileRec.FileName;

            safeName = Path.GetFileName(safeName); // avoid path traversal
            var filePath = Path.Combine(_mergedFilesPath, safeName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on disk.");

            await _fileRepository.UpdateFileDownloadLinkAsync(fileId, safeName);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            const string xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(fileBytes, xlsx, safeName);
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


    }
}
