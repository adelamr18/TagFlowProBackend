public class AddFileDto
{
    public string AddedFileName { get; set; } = "";
    public string FileStatus { get; set; } = "";
    public int FileRowsCount { get; set; } = 0;
    public string UploadedByUserName { get; set; } = "";
    public IFormFile File { get; set; } = null!;
    public int? SelectedProjectId { get; set; }
    public List<int>? SelectedPatientTypeIds { get; set; } = new List<int>();
    public int UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime FileUploadedOn { get; set; }
}
