using OfficeOpenXml;
using TagFlowApi.Models;
using TagFlowApi.Infrastructure;
using TagFlowApi.DTOs;
using Microsoft.EntityFrameworkCore;
using File = TagFlowApi.Models.File;
using TagFlowApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace TagFlowApi.Repositories
{
    public class FileRepository
    {
        private readonly DataContext _context;
        private static readonly string SSN_COLUMN = "ssn";
        private static readonly string PROCESSED_STATUS = "Processed";
        private static readonly string PROCESSED_WITH_ERROR = "Processed_with_error";
        private static readonly string PROCESSING_STATUS = "Processing";
        private static readonly string UNPROCESSED_STATUS = "Unprocessed";
        private static readonly string BASE_URL = "http://localhost:5500";
        public FileRepository(DataContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<List<FileRow>> GetDuplicateSSNsAsync(List<string> ssnIds)
        {
            return await _context.FileRows
                .Where(fr => (ssnIds.Contains(fr.SsnId) && fr.Status == PROCESSED_STATUS) || ssnIds.Contains(fr.SsnId))
                .ToListAsync();
        }

        public async Task<(string? filePath, int newFileId)> UploadFileAsync(AddFileDto fileDto, Stream fileStream)
        {
            if (!fileStream.CanRead)
                throw new Exception("Invalid file stream. The file cannot be read.");

            var (isExcel, hasSsnColumn, _) = ValidateExcelFile(fileStream);
            if (!isExcel)
                throw new Exception("The uploaded file is not a valid Excel file.");
            if (!hasSsnColumn)
                throw new Exception("The uploaded Excel file does not contain a column named 'SSN'.");

            var ssnIds = await ExtractSsnIdsFromExcel(fileStream);

            if (fileStream == null || fileStream.Length == 0)
            {
                throw new Exception("The file stream is empty or null.");
            }

            byte[] fileContent;
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }
            using (var memoryStream = new MemoryStream())
            {
                await fileStream.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            var newFile = new File
            {
                FileName = fileDto.AddedFileName,
                CreatedAt = DateTime.UtcNow,
                FileStatus = fileDto.FileStatus,
                FileRowsCounts = ssnIds.Count,
                UploadedByUserName = fileDto.UploadedByUserName,
                FileContent = fileContent,
                IsUploadedByAdmin = fileDto.IsAdmin,
                FileUploadedOn = fileDto.FileUploadedOn
            };

            if (fileDto.IsAdmin)
            {
                var admin = await _context.Admins.FirstOrDefaultAsync(a => a.AdminId == fileDto.UserId);
                if (admin == null)
                    throw new Exception("Admin user not found in Admins table.");
                newFile.AdminId = admin.AdminId;
            }
            else
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == fileDto.UserId);
                if (user == null)
                    throw new Exception("User not found.");
                newFile.UserId = user.UserId;
                newFile.AdminId = null;
            }

            _context.Files.Add(newFile);
            await _context.SaveChangesAsync();

            var existingDuplicates = await GetDuplicateSSNsAsync(ssnIds);
            var existingSsnMap = existingDuplicates
                .GroupBy(d => d.SsnId)
                .ToDictionary(g => g.Key, g => g.Any(d => d.Status == PROCESSED_STATUS) ? PROCESSED_STATUS : g.First().Status);
            var fileRows = ssnIds.Select(ssn =>
            {
                var status = existingSsnMap.TryGetValue(ssn, out var existingStatus) && existingStatus == PROCESSED_STATUS
                    ? PROCESSED_STATUS
                    : UNPROCESSED_STATUS;
                return new FileRow
                {
                    FileId = newFile.FileId,
                    SsnId = ssn,
                    Status = status
                };
            }).ToList();
            _context.FileRows.AddRange(fileRows);

            if (fileDto.SelectedProjectId.HasValue)
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectId == fileDto.SelectedProjectId.Value);
                if (project != null && !newFile.Projects.Contains(project))
                {
                    newFile.Projects.Add(project);
                }
            }

            if (fileDto.SelectedPatientTypeIds != null && fileDto.SelectedPatientTypeIds.Any())
            {
                var pts = await _context.PatientTypes.Where(pt => fileDto.SelectedPatientTypeIds.Contains(pt.PatientTypeId)).ToListAsync();
                foreach (var pt in pts)
                {
                    if (!newFile.PatientTypes.Contains(pt))
                    {
                        newFile.PatientTypes.Add(pt);
                    }
                }
            }

            if (ssnIds.All(ssn => existingSsnMap.TryGetValue(ssn, out var status) && status == PROCESSED_STATUS))
                newFile.FileStatus = PROCESSED_STATUS;

            await _context.SaveChangesAsync();

            string? mergedFileName = existingDuplicates.Count > 0
                ? await GenerateMergedExcelFileAsync(newFile.FileId, fileDto.File)
                : null;
            await UpdateFileDownloadLinkAsync(newFile.FileId, mergedFileName);

            return (mergedFileName, newFile.FileId);
        }

        private static (bool isExcel, bool hasSsnColumn, List<string> headers) ValidateExcelFile(Stream fileStream)
        {
            using var package = new ExcelPackage(fileStream);
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                return (false, false, new List<string>());
            }

            var headers = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text.Trim());
            }

            var hasSsnColumn = headers.Any(header => string.Equals(header, "SSN", StringComparison.OrdinalIgnoreCase));

            return (true, hasSsnColumn, headers);
        }

        public async Task UpdateFileDownloadLinkAsync(int fileId, string? fileName)
        {
            var fileRecord = await _context.Files.FirstOrDefaultAsync(f => f.FileId == fileId);

            if (fileRecord == null)
            {
                throw new InvalidOperationException($"File with ID {fileId} not found.");
            }

            string? downloadLink = fileName != null
                ? $"{BASE_URL}/api/file/download?fileName={Uri.EscapeDataString(fileName)}&fileId={fileId}"
                : null;

            fileRecord.DownloadLink = downloadLink ?? "";
            bool allRowsProcessed = await _context.FileRows
                .Where(fr => fr.FileId == fileId)
                .AllAsync(fr => fr.Status.ToLower() == PROCESSED_STATUS.ToLower() ||
                                  fr.Status.ToLower() == PROCESSED_WITH_ERROR.ToLower());

            if (allRowsProcessed)
            {
                fileRecord.FileStatus = PROCESSED_STATUS;
            }
            else
            {
                fileRecord.FileStatus = UNPROCESSED_STATUS;
            }


            _context.Files.Update(fileRecord);
            await _context.SaveChangesAsync();
        }

        public async Task<List<string>> ExtractSsnIdsFromExcel(Stream fileStream)
        {
            var ssnIds = new List<string>();

            using (var package = new ExcelPackage(fileStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null)
                {
                    throw new Exception("The Excel file does not contain any worksheet.");
                }

                var headerRow = worksheet.Cells[1, 1, 1, worksheet.Dimension.Columns];
                int ssnColumnIndex = -1;

                foreach (var cell in headerRow)
                {
                    if (cell.Text.Trim().ToLower() == SSN_COLUMN)
                    {
                        ssnColumnIndex = cell.Start.Column;
                        break;
                    }
                }

                if (ssnColumnIndex == -1)
                {
                    throw new Exception("The Excel file must contain a column named 'ssn'.");
                }

                for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                {
                    var ssnValue = worksheet.Cells[row, ssnColumnIndex].Text.Trim();
                    if (!string.IsNullOrEmpty(ssnValue))
                    {
                        ssnIds.Add(ssnValue);
                    }
                }
            }

            return await Task.FromResult(ssnIds);
        }

        public async Task<List<FileRow>> FetchAndLockUnprocessedRowsAsync(int batchSize)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var unprocessedRows = await _context.FileRows
                    .Where(fr => fr.Status == UNPROCESSED_STATUS)
                    .OrderBy(fr => fr.FileRowId)
                    .Take(batchSize)
                    .ToListAsync();

                if (unprocessedRows.Any())
                {
                    foreach (var row in unprocessedRows)
                    {
                        row.Status = PROCESSING_STATUS;
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return unprocessedRows;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task ProcessExpiredSsnIdsAsync(int fileId, IHubContext<FileStatusHub> hubContext)
        {
            var todayString = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var expiredRows = await _context.FileRows
       .Where(fr => fr.FileId == fileId &&
                    !string.IsNullOrEmpty(fr.InsuranceExpiryDate) &&
                    fr.InsuranceExpiryDate.CompareTo(todayString) < 0)
       .ToListAsync();

            if (expiredRows.Any())
            {
                foreach (var row in expiredRows)
                {
                    var expiredRecord = new ExpiredSsnIds
                    {
                        FileRowId = row.FileRowId,
                        SsnId = row.SsnId,
                        FileRowInsuranceExpiryDate = DateTime.Parse(row.InsuranceExpiryDate).ToUniversalTime().ToString("yyyy-MM-dd"),
                        FileId = row.FileId,
                        ExpiredAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    _context.ExpiredSsnIds.Add(expiredRecord);
                    _context.FileRows.Remove(row);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateProcessedDataAsync(int fileId, List<FileRowDto> updates, IHubContext<FileStatusHub> hubContext)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var updateIds = updates.Select(u => u.FileRowId).ToList();
                var rowsToUpdate = await _context.FileRows
                    .Where(fr => fr.FileId == fileId && updateIds.Contains(fr.FileRowId))
                    .ToListAsync();

                if (!rowsToUpdate.Any())
                    throw new Exception("No rows found to update.");

                var rowDict = rowsToUpdate.ToDictionary(fr => fr.FileRowId);

                foreach (var update in updates)
                {
                    if (rowDict.TryGetValue(update.FileRowId, out var fileRow))
                    {
                        if (!string.IsNullOrWhiteSpace(update.InsuranceExpiryDate) &&
                            DateTime.TryParse(update.InsuranceExpiryDate, out var parsedExpiryDateExpired) &&
                            parsedExpiryDateExpired < DateTime.UtcNow.Date)
                        {
                            var expiredRecord = new ExpiredSsnIds
                            {
                                FileRowId = fileRow.FileRowId,
                                SsnId = update.Ssn,
                                FileRowInsuranceExpiryDate = parsedExpiryDateExpired.ToString("yyyy-MM-dd"),
                                FileId = fileRow.FileId,
                                ExpiredAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                            };
                            _context.ExpiredSsnIds.Add(expiredRecord);
                            _context.FileRows.Remove(fileRow);
                        }
                        else
                        {
                            fileRow.Status = update.Status;
                            fileRow.SsnId = update.Ssn;
                            fileRow.InsuranceCompany = update.InsuranceCompany;
                            fileRow.MedicalNetwork = update.MedicalNetwork;
                            fileRow.IdentityNumber = update.IdentityNumber;
                            fileRow.PolicyNumber = update.PolicyNumber;
                            fileRow.Class = update.Class;
                            fileRow.DeductIblerate = update.DeductIblerate;
                            fileRow.MaxLimit = update.MaxLimit;
                            if (!string.IsNullOrWhiteSpace(update.UploadDate) &&
                                DateTime.TryParse(update.UploadDate, out var parsedUploadDate))
                                fileRow.UploadDate = parsedUploadDate.ToUniversalTime().ToString("yyyy-MM-dd");
                            if (!string.IsNullOrWhiteSpace(update.InsuranceExpiryDate) &&
                                DateTime.TryParse(update.InsuranceExpiryDate, out var parsedExpiryDateRow))
                                fileRow.InsuranceExpiryDate = parsedExpiryDateRow.ToUniversalTime().ToString("yyyy-MM-dd");
                            fileRow.BeneficiaryType = update.BeneficiaryType;
                            fileRow.BeneficiaryNumber = update.BeneficiaryNumber;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            var processed = PROCESSED_STATUS.ToLower();
            var processedWithError = PROCESSED_WITH_ERROR.ToLower();

            var anyProcessed = await _context.FileRows
                .Where(fr => fr.FileId == fileId &&
                    (fr.Status.ToLower() == processed || fr.Status.ToLower() == processedWithError))
                .AnyAsync();

            var file = await _context.Files.FindAsync(fileId);
            if (file == null)
                throw new Exception("File not found.");

            if (anyProcessed)
            {
                if (file.FileContent == null)
                    throw new Exception("The file content is null.");

                using (var fileStream = new MemoryStream(file.FileContent))
                {
                    IFormFile newFormFile = ConvertToIFormFile(fileStream, file.FileName);
                    string mergedFilePath;
                    if (!string.IsNullOrEmpty(file.DownloadLink))
                    {
                        using (var existingFileStream = new MemoryStream(file.FileContent))
                        {
                            IFormFile existingFormFile = ConvertToIFormFile(existingFileStream, "ExistingFile.xlsx");
                            string tempFilePath = Path.Combine(Path.GetTempPath(), existingFormFile.FileName);
                            using (var tempFileStream = new FileStream(tempFilePath, FileMode.Create))
                            {
                                await existingFormFile.CopyToAsync(tempFileStream);
                            }
                            mergedFilePath = await GenerateMergedExcelFileAsync(fileId, newFormFile, tempFilePath);
                        }
                    }
                    else
                    {
                        mergedFilePath = await GenerateMergedExcelFileAsync(fileId, newFormFile);
                    }
                    await UpdateFileDownloadLinkAsync(fileId, mergedFilePath);
                    file.DownloadLink = mergedFilePath;
                }
            }
            else
            {
                file.FileStatus = UNPROCESSED_STATUS;
                await _context.SaveChangesAsync();
            }

            await hubContext.Clients.All.SendAsync("FileStatusUpdated", fileId, file.DownloadLink, file.FileStatus);
        }



        private static IFormFile ConvertToIFormFile(MemoryStream memoryStream, string fileName)
        {
            if (memoryStream == null)
            {
                throw new ArgumentNullException(nameof(memoryStream), "MemoryStream cannot be null.");
            }

            memoryStream.Position = 0;
            return new FormFile(memoryStream, 0, memoryStream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
        }

        private async Task<string> GenerateMergedExcelFileAsync(int fileId, IFormFile originalFile, string existingFilePath = "")
        {
            // Added "FileRowStatus" as a new column
            var dbHeaders = new[]
            {
            "InsuranceCompany", "MedicalNetwork", "IdentityNumber",
            "PolicyNumber", "Class", "DeductIblerate", "MaxLimit",
            "UploadDate", "InsuranceExpiryDate", "BeneficiaryType", "BeneficiaryNumber",
            "FileRowStatus"
          };

            var uploadedData = ReadOriginalExcelDataAsync(originalFile);

            var originalSsnIds = uploadedData
                .Select(row => row.ContainsKey("ssn") ? row["ssn"] : null)
                .Where(ssn => !string.IsNullOrEmpty(ssn))
                .ToHashSet();

            var dbRows = await _context.FileRows
                .Where(fr => originalSsnIds.Contains(fr.SsnId) &&
                    (fr.Status.ToLower() == PROCESSED_STATUS.ToLower() || fr.Status.ToLower() == PROCESSED_WITH_ERROR.ToLower()))
                .Distinct()
                .ToListAsync();

            List<Dictionary<string, string>> existingData = new List<Dictionary<string, string>>();
            if (!string.IsNullOrEmpty(existingFilePath) && System.IO.File.Exists(existingFilePath))
            {
                existingData = ReadExcelDataFromFilePathAsync(existingFilePath);
            }
            var mergedData = new List<Dictionary<string, string>>();
            mergedData.AddRange(uploadedData);
            foreach (var existingRow in existingData)
            {
                if (existingRow.ContainsKey("ssn") && !originalSsnIds.Contains(existingRow["ssn"]))
                {
                    mergedData.Add(existingRow);
                }
            }

            var directoryPath = Path.Combine(Path.GetTempPath(), "MergedFiles");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var fileName = $"File_{fileId}_Merged.xlsx";
            var filePath = Path.Combine(directoryPath, fileName);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Merged Data");

            var dynamicHeaders = uploadedData.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
            var allHeaders = dynamicHeaders.Concat(dbHeaders).ToList();

            for (int col = 0; col < allHeaders.Count; col++)
            {
                worksheet.Cells[1, col + 1].Value = allHeaders[col];
            }

            int rowNumber = 2;
            foreach (var row in mergedData)
            {
                int colIndex = 1;
                foreach (var header in dynamicHeaders)
                {
                    worksheet.Cells[rowNumber, colIndex].Value = row.ContainsKey(header) ? row[header] : null;
                    colIndex++;
                }

                var ssnId = row.ContainsKey("ssn") ? row["ssn"] : null;
                if (!string.IsNullOrEmpty(ssnId))
                {
                    var matchingDbRow = dbRows.FirstOrDefault(dbRow => dbRow.SsnId == ssnId);
                    if (matchingDbRow != null)
                    {
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 1].Value = matchingDbRow.InsuranceCompany;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 2].Value = matchingDbRow.MedicalNetwork;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 3].Value = matchingDbRow.IdentityNumber;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 4].Value = matchingDbRow.PolicyNumber;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 5].Value = matchingDbRow.Class;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 6].Value = matchingDbRow.DeductIblerate;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 7].Value = matchingDbRow.MaxLimit;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 8].Value = matchingDbRow.UploadDate;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 9].Value = matchingDbRow.InsuranceExpiryDate;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 10].Value = matchingDbRow.BeneficiaryType;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 11].Value = matchingDbRow.BeneficiaryNumber;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 12].Value = matchingDbRow.Status;
                    }
                }

                rowNumber++;
            }

            try
            {
                await package.SaveAsAsync(new FileInfo(filePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file: {ex.Message}");
            }

            return fileName;
        }


        private static List<Dictionary<string, string>> ReadExcelDataFromFilePathAsync(string filePath)
        {
            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();

            if (worksheet == null) return new List<Dictionary<string, string>>();

            var headers = new List<string>();
            for (int col = 1; col <= worksheet.Dimension.Columns; col++)
            {
                headers.Add(worksheet.Cells[1, col].Text.Trim());
            }

            var data = new List<Dictionary<string, string>>();
            for (int row = 2; row <= worksheet.Dimension.Rows; row++)
            {
                var rowData = new Dictionary<string, string>();
                for (int col = 1; col <= headers.Count; col++)
                {
                    rowData[headers[col - 1]] = worksheet.Cells[row, col].Text.Trim();
                }
                data.Add(rowData);
            }

            return data;
        }

        private static List<Dictionary<string, string>> ReadOriginalExcelDataAsync(IFormFile file)
        {
            var result = new List<Dictionary<string, string>>();

            using (var stream = file.OpenReadStream())
            {
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension.Rows;
                var columnCount = worksheet.Dimension.Columns;

                var header = new List<string>();
                for (int col = 1; col <= columnCount; col++)
                {
                    header.Add(worksheet.Cells[1, col].Text);
                }

                for (int row = 2; row <= rowCount; row++)
                {
                    var rowData = new Dictionary<string, string>();
                    for (int col = 1; col <= columnCount; col++)
                    {
                        rowData[header[col - 1]] = worksheet.Cells[row, col].Text;
                    }
                    result.Add(rowData);
                }
            }

            return result;
        }

        public async Task<List<FileDto>> GetAllFilesAsync()
        {
            var files = await _context.Files
                .AsNoTracking()
                .Select(f => new FileDto
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    UploadedByUserName = f.UploadedByUserName,
                    CreatedAt = f.CreatedAt,
                    FileStatus = f.FileStatus,
                    FileRowsCounts = f.FileRowsCounts,
                    DownloadLink = f.DownloadLink,
                    SelectedProjectId = f.Projects.Any() ? f.Projects.Select(p => p.ProjectId).FirstOrDefault() : (int?)null,
                    SelectedPatientTypeIds = f.PatientTypes.Select(pt => pt.PatientTypeId).ToList(),
                    UserId = f.UserId ?? 0,
                    IsAdmin = f.IsUploadedByAdmin,
                    FileUploadedOn = f.FileUploadedOn
                })
                .ToListAsync();
            return files;
        }

        public async Task<FileDto?> GetFileByIdAsync(int fileId)
        {
            return await _context.Files
                .Where(f => f.FileId == fileId)
                .Select(f => new FileDto
                {
                    FileId = f.FileId,
                    FileName = f.FileName
                })
                .FirstOrDefaultAsync();
        }

        public async Task DeleteFileAsync(int fileId)
        {
            var file = await _context.Files.FirstOrDefaultAsync(f => f.FileId == fileId);
            if (file != null)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }
        }
    }
}
