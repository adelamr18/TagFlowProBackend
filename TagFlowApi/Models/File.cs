using System.ComponentModel.DataAnnotations.Schema;

namespace TagFlowApi.Models
{
    public class File
    {
        public int FileId { get; set; }
        public string FileName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string FileStatus { get; set; } = "";
        public int FileRowsCounts { get; set; } = 0;
        public string UploadedByUserName { get; set; } = "";
        public string DownloadLink { get; set; } = "";
        public byte[]? FileContent { get; set; }
        public int? UserId { get; set; }
        public bool IsUploadedByAdmin { get; set; } = false;

        [ForeignKey("UploadedAdmin")]
        public int? AdminId { get; set; }
        public Admin? UploadedAdmin { get; set; }
        public DateTime FileUploadedOn { get; set; }

        public ICollection<FileTag> FileTags { get; set; } = new List<FileTag>();
        public ICollection<FileRow> FileRows { get; set; } = new List<FileRow>();
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<PatientType> PatientTypes { get; set; } = new List<PatientType>();

        public ICollection<RobotErrors> RobotErrors { get; set; } = new List<RobotErrors>();
    }
}
