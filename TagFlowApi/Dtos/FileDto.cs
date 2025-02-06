public class FileDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = "";
    public string UploadedByUserName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string FileStatus { get; set; } = "";
    public int FileRowsCounts { get; set; }
    public string DownloadLink { get; set; } = "";
    public int? SelectedProjectId { get; set; }
    public List<int>? SelectedPatientTypeIds { get; set; } = new List<int>();
    public int UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime FileUploadedOn { get; set; }

}
