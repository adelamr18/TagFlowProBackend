namespace TagFlowApi.Models
{
    public class Tag
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = "";
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        // Foreign key to Admin table (creator of the tag)
        public int CreatedBy { get; set; }
        public Admin? CreatedByAdmin { get; set; }
        // One-to-many relationship with TagValues
        public ICollection<TagValue> TagValues { get; set; } = new List<TagValue>();
        // Many-to-many relationship with Users through UserTagPermissions
        public ICollection<UserTagPermission> UserTagPermissions { get; set; } = new List<UserTagPermission>();
        // Many-to-many relationship with Files through FileTags
        public ICollection<FileTag> FileTags { get; set; } = new List<FileTag>();
        public string UpdatedBy { get; set; } = "";
    }
}
