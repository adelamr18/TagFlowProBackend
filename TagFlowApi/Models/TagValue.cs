namespace TagFlowApi.Models
{
    public class TagValue
    {
        public int TagValueId { get; set; }
        public string Value { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        // Foreign key to Tag table
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;

        // Foreign key to Admin table (creator of the tag value)
        public int CreatedBy { get; set; }
        public Admin? CreatedByAdmin { get; set; }

        // Many-to-many relationship with FileTags (instead of FileRows)
        public ICollection<FileTag> FileTags { get; set; } = new List<FileTag>();
    }
}
