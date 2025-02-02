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
       [FromForm] string selectedTags,
       [FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No file provided." });
                }

                var deserializedTags = string.IsNullOrEmpty(selectedTags)
                    ? new List<TagDto>()
                    : JsonConvert.DeserializeObject<List<TagDto>>(selectedTags);

                var addFileDto = new AddFileDto
                {
                    AddedFileName = addedFileName,
                    FileStatus = fileStatus,
                    FileRowsCount = fileRowsCount,
                    UploadedByUserName = uploadedByUserName,
                    SelectedTags = deserializedTags,
                    File = file
                };

                using (var fileStream = file.OpenReadStream())
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
        public async Task<IActionResult> UpdateProcessedData(int fileId, List<FileRowDto> processedFileRows)
        {
            try
            {
                await _fileRepository.UpdateProcessedDataAsync(fileId, processedFileRows, _hubContext);

                return Ok(new { success = true, message = "Data updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadMergedFile([FromQuery] string fileName, [FromQuery] int fileId)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name is required.");
            }

            var filePath = Path.Combine(_mergedFilesPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            await _fileRepository.UpdateFileDownloadLinkAsync(fileId, fileName);

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            return File(fileBytes, contentType, fileName);
        }

        [HttpGet("get-all-files")]
        public async Task<IActionResult> GetAllFiles()
        {
            try
            {
                var files = await _fileRepository.GetAllFilesAsync();

                if (files == null || !files.Any())
                {
                    return NotFound(new { success = false, message = "No files found." });
                }

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


    }
}
