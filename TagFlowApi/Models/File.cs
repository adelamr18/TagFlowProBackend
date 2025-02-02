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

        // One-to-many relationship with FileTags
        public ICollection<FileTag> FileTags { get; set; } = new List<FileTag>();
        public ICollection<FileRow> FileRows { get; set; } = new List<FileRow>();
    }
}
