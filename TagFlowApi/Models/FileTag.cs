using System.ComponentModel.DataAnnotations.Schema;

namespace TagFlowApi.Models
{
    public class FileTag
    {
        public int FileTagId { get; set; }
        public int FileId { get; set; }
        public int TagId { get; set; }
        public List<int> TagValuesIds { get; set; } = new List<int>();

        // Navigation properties
        public File File { get; set; } = null!;
        public Tag Tag { get; set; } = null!;
    }
}
