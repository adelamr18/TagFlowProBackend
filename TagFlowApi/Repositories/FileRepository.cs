using OfficeOpenXml;
using TagFlowApi.Models;
using TagFlowApi.Infrastructure;
using TagFlowApi.DTOs;
using Microsoft.EntityFrameworkCore;
using File = TagFlowApi.Models.File;
using TagFlowApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

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
        private static readonly string BASE_URL = "http://172.29.2.2:8080";
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
          bool isDuplicateProcessed = existingSsnMap.TryGetValue(ssn, out var existingStatus) && existingStatus == PROCESSED_STATUS;
          var status = isDuplicateProcessed ? PROCESSED_STATUS : UNPROCESSED_STATUS;
          var fileRow = new FileRow
          {
              FileId = newFile.FileId,
              SsnId = ssn,
              Status = status,
          };
          if (isDuplicateProcessed)
          {
              var duplicate = existingDuplicates.FirstOrDefault(d => d.SsnId == ssn);
              if (duplicate != null)
              {
                  fileRow.InsuranceCompany = duplicate.InsuranceCompany;
                  fileRow.MedicalNetwork = duplicate.MedicalNetwork;
                  fileRow.IdentityNumber = duplicate.IdentityNumber;
                  fileRow.PolicyNumber = duplicate.PolicyNumber;
                  fileRow.Class = duplicate.Class;
                  fileRow.DeductIblerate = duplicate.DeductIblerate;
                  fileRow.MaxLimit = duplicate.MaxLimit;
              }
          }
          return fileRow;
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

            string? downloadLink = null;
            if (fileName != null)
            {
                var apiKey = Environment.GetEnvironmentVariable("API_KEY") ?? Environment.GetEnvironmentVariable("ApiKey");
                var qs = $"fileName={Uri.EscapeDataString(fileName)}&fileId={fileId}";
                downloadLink = string.IsNullOrEmpty(apiKey)
                    ? $"{BASE_URL}/api/file/download?{qs}"
                    : $"{BASE_URL}/api/file/download?{qs}&apiKey={Uri.EscapeDataString(apiKey)}";
            }

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

        public async Task UpdateProcessedDataAsync(int fileId, [FromBody] List<FileRowDto> updates, IHubContext<FileStatusHub> hubContext)
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
                                DateTime.TryParse(update.InsuranceExpiryDate, out var parsedExpiryDateForRow))
                                fileRow.InsuranceExpiryDate = parsedExpiryDateForRow.ToUniversalTime().ToString("yyyy-MM-dd");
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
            var dbHeaders = new[]
            {
            "InsuranceCompany", "MedicalNetwork", "IdentityNumber",
            "PolicyNumber", "Class", "DeductIblerate", "MaxLimit",
            "UploadDate", "insuranceExpiryDate", "beneficiaryType", "beneficiaryNumber",
            "Gender","FileRowStatus"
            };

            var uploadedData = ReadOriginalExcelDataAsync(originalFile);
            var uploadedDataLower = uploadedData.Select(dict =>
                dict.ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value)
            ).ToList();

            var originalSsnIds = uploadedDataLower
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
                existingData = existingData.Select(dict =>
                    dict.ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value)
                ).ToList();
            }

            var mergedData = new List<Dictionary<string, string>>();
            mergedData.AddRange(uploadedDataLower);
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

            var dynamicHeaders = uploadedDataLower.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
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
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 12].Value = matchingDbRow.Gender;
                        worksheet.Cells[rowNumber, dynamicHeaders.Count + 13].Value = matchingDbRow.Status;
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
                .
                FirstOrDefaultAsync();
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
        public async Task<OverviewDto> GetOverviewAsync(DateTime? fromDate, DateTime? toDate, string? projectName, string? patientType, int? viewerId = null)
        {
            var normalizedProjectName = string.IsNullOrWhiteSpace(projectName) ? "" : projectName.ToLower().Replace(" ", "");
            var normalizedPatientType = string.IsNullOrWhiteSpace(patientType) ? "" : patientType.ToLower().Replace(" ", "");

            // Prepare date boundaries.
            DateTime? startDate = null;
            DateTime? endDate = null;
            if (fromDate.HasValue)
            {
                startDate = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
            }
            if (toDate.HasValue)
            {
                // Add one day so that the entire 'to' date is included.
                endDate = DateTime.SpecifyKind(toDate.Value.Date, DateTimeKind.Utc).AddDays(1);
            }

            // --------------------
            // 1. Overall KPI Counts
            // -
            var overviewCounts = await (
                from f in _context.Files
                where ((startDate == null || f.FileUploadedOn >= startDate)
                       && (endDate == null || f.FileUploadedOn < endDate))
                      && (string.IsNullOrEmpty(normalizedProjectName)
                          || f.Projects.Any(p => p.ProjectName.ToLower().Replace(" ", "") == normalizedProjectName))
                      && (string.IsNullOrEmpty(normalizedPatientType)
                          || f.PatientTypes.Any(pt => pt.Name.ToLower().Replace(" ", "") == normalizedPatientType))
                join fr in _context.FileRows on f.FileId equals fr.FileId
                orderby fr.FileRowId
                group fr by 1 into g
                select new OverviewDto
                {
                    InsuredPatients = g.Sum(fr => (fr.Status == PROCESSED_STATUS || fr.Status == PROCESSED_WITH_ERROR)
                                                    ? (!string.IsNullOrEmpty(fr.InsuranceCompany) ? 1 : 0)
                                                    : 0),
                    NonInsuredPatients = g.Sum(fr => (fr.Status == PROCESSED_STATUS || fr.Status == PROCESSED_WITH_ERROR)
                                                       ? (string.IsNullOrEmpty(fr.InsuranceCompany) ? 1 : 0)
                                                       : 0),
                    SaudiPatients = g.Sum(fr => !string.IsNullOrEmpty(fr.SsnId) && fr.SsnId.StartsWith("1") ? 1 : 0),
                    NonSaudiPatients = g.Sum(fr => string.IsNullOrEmpty(fr.SsnId) || !fr.SsnId.StartsWith("1") ? 1 : 0)
                }
            ).OrderBy(x => x.InsuredPatients).FirstOrDefaultAsync();

            // Initialize overview DTO.
            var overviewDto = overviewCounts ?? new OverviewDto();

            // -------------------------
            // 3. Projects Per Patient Analytics
            // -------------------------
            var fileRowsQuery = _context.FileRows
      .Include(fr => fr.File)
          .ThenInclude(f => f.Projects)
      .Where(fr =>
          (startDate == null || fr.File.FileUploadedOn >= startDate) &&
          (endDate == null || fr.File.FileUploadedOn < endDate) &&
          (string.IsNullOrEmpty(normalizedProjectName) ||
              fr.File.Projects.Any(p => p.ProjectName.ToLower().Replace(" ", "") == normalizedProjectName)) &&
          (string.IsNullOrEmpty(normalizedPatientType) ||
              fr.File.PatientTypes.Any(pt => pt.Name.ToLower().Replace(" ", "") == normalizedPatientType)) &&
          (viewerId == null ||
              fr.File.UserId == viewerId)

      );

            var projectFileRows = await fileRowsQuery
                .SelectMany(fr => fr.File.Projects.Select(p => new { ProjectName = p.ProjectName, fr }))
                .ToListAsync();

            var projectAnalytics = projectFileRows
                .GroupBy(x => x.ProjectName)
                .Select(g => new
                {
                    ProjectName = g.Key,
                    InsuredCount = g.Sum(x => (x.fr.Status == PROCESSED_STATUS || x.fr.Status == PROCESSED_WITH_ERROR) && !string.IsNullOrEmpty(x.fr.InsuranceCompany) ? 1 : 0),
                    NonInsuredCount = g.Sum(x => (x.fr.Status == PROCESSED_STATUS || x.fr.Status == PROCESSED_WITH_ERROR) && string.IsNullOrEmpty(x.fr.InsuranceCompany) ? 1 : 0)
                })
                .ToList();

            var expiredAnalytics = await _context.Files
                .Include(f => f.Projects)
                .Where(f =>
                    (startDate == null || f.FileUploadedOn >= startDate) &&
                    (endDate == null || f.FileUploadedOn < endDate) &&
                    (string.IsNullOrEmpty(normalizedProjectName) ||
                        f.Projects.Any(p => p.ProjectName.ToLower().Replace(" ", "") == normalizedProjectName)) &&
                    (string.IsNullOrEmpty(normalizedPatientType) ||
                        f.PatientTypes.Any(pt => pt.Name.ToLower().Replace(" ", "") == normalizedPatientType)) &&
                    (viewerId == null ||
                        f.UserId == viewerId)

                )
                .Join(_context.ExpiredSsnIds, f => f.FileId, es => es.FileId, (f, es) => f)
                .SelectMany(f => f.Projects.Select(p => new { p.ProjectName, Count = 1 }))
                .GroupBy(x => x.ProjectName)
                .Select(g => new { ProjectName = g.Key, ExpiredCount = g.Sum(x => x.Count) })
                .ToListAsync();

            var projectsAnalyticsMerged = projectAnalytics.GroupJoin(
                expiredAnalytics,
                fa => fa.ProjectName,
                ex => ex.ProjectName,
                (fa, ex) => new
                {
                    fa.ProjectName,
                    InsuredCount = fa.InsuredCount,
                    NonInsuredCount = fa.NonInsuredCount,
                    ExpiredCount = ex.Select(x => x.ExpiredCount).FirstOrDefault()
                }
            ).ToList();

            var overallTotal = projectsAnalyticsMerged.Sum(p => p.InsuredCount + p.NonInsuredCount + p.ExpiredCount);
            var projectsPerPatientAnalytics = projectsAnalyticsMerged.Select(p => new ProjectPatientAnalyticsDto
            {
                ProjectName = p.ProjectName,
                InsuredPatients = p.InsuredCount,
                NonInsuredPatients = p.NonInsuredCount,
                TotalPatients = p.InsuredCount + p.NonInsuredCount + p.ExpiredCount,
                PercentageOfPatientsPerProject = overallTotal > 0
                    ? Math.Round((double)(p.InsuredCount + p.NonInsuredCount + p.ExpiredCount) / overallTotal * 100, 2)
                    : 0
            }).ToList();

            overviewDto.ProjectsPerPatientAnalytics = projectsPerPatientAnalytics;
            // -------------------------
            // 4. Insurance Companies Per Patient Analytics
            // -------------------------
            // Query for file rows that have an insurance company, are processed, and match the filters.
            var insuranceQuery = _context.FileRows
            .Include(fr => fr.File)
            .Where(fr =>
                (startDate == null || fr.File.FileUploadedOn >= startDate) &&
                (endDate == null || fr.File.FileUploadedOn < endDate) &&
                (string.IsNullOrEmpty(normalizedProjectName) || fr.File.Projects.Any(p => p.ProjectName.ToLower().Replace(" ", "") == normalizedProjectName)) &&
                (string.IsNullOrEmpty(normalizedPatientType) || fr.File.PatientTypes.Any(pt => pt.Name.ToLower().Replace(" ", "") == normalizedPatientType)) &&
                (fr.Status == PROCESSED_STATUS || fr.Status == PROCESSED_WITH_ERROR) &&
                !string.IsNullOrEmpty(fr.InsuranceCompany) &&
                (viewerId == null || fr.File.UserId == viewerId)
            );

            var insuranceAnalytics = await insuranceQuery
                .GroupBy(fr => fr.InsuranceCompany)
                .Select(g => new
                {
                    InsuranceCompany = g.Key,
                    InsuredCount = g.Count()
                })
                .ToListAsync();

            var overallInsuranceTotal = insuranceAnalytics.Sum(x => x.InsuredCount);
            var insuranceCompaniesPertPatientAnalytics = insuranceAnalytics.Select(x => new InsuranceCompanyPatientAnalyticsDto
            {
                InsuranceCompany = x.InsuranceCompany,
                InsuredPatients = x.InsuredCount,
                PercentageOfPatients = overallInsuranceTotal > 0
                    ? Math.Round((double)x.InsuredCount / overallInsuranceTotal * 100, 2)
                    : 0
            }).ToList();

            overviewDto.InsuranceCompaniesPertPatientAnalytics = insuranceCompaniesPertPatientAnalytics;

            return overviewDto;
        }

        public async Task<List<ProjectAnalyticsDto>> GetProjectAnalyticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        string? projectName,
        string? patientType,
        string? timeGranularity)
        {
            var normalizedProjectName = string.IsNullOrWhiteSpace(projectName)
                ? ""
                : projectName.ToLower().Replace(" ", "");

            var normalizedPatientType = string.IsNullOrWhiteSpace(patientType)
                ? ""
                : patientType.ToLower().Replace(" ", "");

            DateTime? startDateBoundary = fromDate.HasValue
                ? DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc)
                : null;
            DateTime? endDateBoundary = toDate.HasValue
                ? DateTime.SpecifyKind(toDate.Value.Date, DateTimeKind.Utc).AddDays(1)
                : null;

            var query = _context.FileRows
                .AsSplitQuery()
                .Include(fr => fr.File)
                    .ThenInclude(f => f.Projects)
                .Include(fr => fr.File.PatientTypes)
                .Where(fr =>
                    (startDateBoundary == null || fr.File!.FileUploadedOn >= startDateBoundary) &&
                    (endDateBoundary == null || fr.File!.FileUploadedOn < endDateBoundary) &&
                    (string.IsNullOrEmpty(normalizedProjectName) ||
                     fr.File!.Projects.Any(p => p.ProjectName.ToLower().Replace(" ", "") == normalizedProjectName)) &&
                    (string.IsNullOrEmpty(normalizedPatientType) ||
                     fr.File!.PatientTypes.Any(pt => pt.Name.ToLower().Replace(" ", "") == normalizedPatientType))
                );

            timeGranularity = string.IsNullOrEmpty(timeGranularity) ? "daily" : timeGranularity.ToLower();

            List<(ProjectTimeKey Key, List<FileRow> Rows)> groupTuples;

            if (timeGranularity == "weekly")
            {
                var fileRows = await query.ToListAsync();

                var weeklyGroups = fileRows.GroupBy(fr =>
                {
                    var date = fr.File!.FileUploadedOn;
                    int simpleWeek = ((date.DayOfYear - 1) / 7) + 1;

                    return new
                    {
                        ProjectName = fr.File.Projects.Select(p => p.ProjectName).FirstOrDefault() ?? "",
                        Year = date.Year,
                        ComputedWeek = simpleWeek
                    };
                });

                groupTuples = weeklyGroups
                    .Select(g =>
                    {
                        var earliestDate = g.Min(r => r.File!.FileUploadedOn);
                        var key = new ProjectTimeKey
                        {
                            ProjectName = g.Key.ProjectName,
                            Year = g.Key.Year,
                            Week = g.Key.ComputedWeek,
                            Date = earliestDate
                        };
                        return (Key: key, Rows: g.ToList());
                    })
                    .ToList();
            }
            else
            {
                IQueryable<IGrouping<ProjectTimeKey, FileRow>> groupedQuery = timeGranularity switch
                {
                    "daily" => query.GroupBy(fr => new ProjectTimeKey
                    {
                        ProjectName = fr.File!.Projects.Select(p => p.ProjectName).FirstOrDefault() ?? "",
                        Date = fr.File!.FileUploadedOn.Date
                    }),
                    "monthly" => query.GroupBy(fr => new ProjectTimeKey
                    {
                        ProjectName = fr.File!.Projects.Select(p => p.ProjectName).FirstOrDefault() ?? "",
                        Year = fr.File!.FileUploadedOn.Year,
                        Month = fr.File!.FileUploadedOn.Month
                    }),
                    "yearly" => query.GroupBy(fr => new ProjectTimeKey
                    {
                        ProjectName = fr.File!.Projects.Select(p => p.ProjectName).FirstOrDefault() ?? "",
                        Year = fr.File!.FileUploadedOn.Year
                    }),
                    _ => query.GroupBy(fr => new ProjectTimeKey
                    {
                        ProjectName = fr.File!.Projects.Select(p => p.ProjectName).FirstOrDefault() ?? "",
                        Date = fr.File!.FileUploadedOn.Date
                    })
                };

                var efGroups = await groupedQuery
                    .Select(g => new
                    {
                        Key = g.Key,
                        Rows = g.ToList()
                    })
                    .ToListAsync();

                groupTuples = efGroups
                    .Select(x => (Key: x.Key, Rows: x.Rows))
                    .ToList();
            }

            var analytics = groupTuples.Select(pair =>
            {
                var key = pair.Key;
                var rows = pair.Rows;

                string timeLabel = timeGranularity switch
                {
                    "daily" => key.Date.HasValue
                        ? key.Date.Value.ToString("yyyy-MM-dd")
                        : "",
                    "weekly" => key.Date.HasValue
                        ? key.Date.Value.ToString("yyyy-MM-dd")
                        : "",
                    "monthly" => key.Year.HasValue
                        ? $"{key.Year}-{(key.Month.HasValue ? key.Month.Value.ToString("D2") : "00")}"
                        : "",
                    "yearly" => key.Year?.ToString() ?? "",
                    _ => key.Date.HasValue
                        ? key.Date.Value.ToString("yyyy-MM-dd")
                        : ""
                };

                return new ProjectAnalyticsDto
                {
                    ProjectName = key.ProjectName,
                    TimeLabel = timeLabel,
                    TotalPatients = rows.Count,
                    InsuredPatients = rows.Count(fr => (fr.Status == PROCESSED_STATUS || fr.Status == PROCESSED_WITH_ERROR)
                                                 && !string.IsNullOrEmpty(fr.InsuranceCompany)),
                    NonInsuredPatients = rows.Count(fr => (fr.Status == PROCESSED_STATUS || fr.Status == PROCESSED_WITH_ERROR)
                                                 && string.IsNullOrEmpty(fr.InsuranceCompany))
                };
            }).ToList();

            return analytics;
        }

        public async Task<int> AddRobotErrorAsync(RobotErrorCreateDto dto)
        {
            File? file = null;
            if (dto.FileId.HasValue)
            {
                file = await _context.Files
                                     .AsNoTracking()
                                     .FirstOrDefaultAsync(f => f.FileId == dto.FileId.Value);

                if (file == null)
                    throw new ArgumentException($"File with id {dto.FileId.Value} was not found.");
            }

            var entity = new RobotErrors
            {
                Module = dto.Module.Trim(),
                ErrorMessage = dto.ErrorMessage,                // keep raw text
                Timestamp = (dto.Timestamp ?? DateTime.UtcNow), // default to now (UTC)
                FileId = dto.FileId,
                FileName = dto.FileName ?? "",
                PatientId = dto.PatientId ?? "",
            };

            _context.RobotErrors.Add(entity);
            await _context.SaveChangesAsync();

            return entity.Id;
        }


    }
}
