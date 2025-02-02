namespace TagFlowApi.DTOs
{
    public class AddFileDto
    {
        public string AddedFileName { get; set; } = "";
        public string FileStatus { get; set; } = "";
        public int FileRowsCount { get; set; } = 0;
        public List<TagDto> SelectedTags { get; set; } = new List<TagDto>();
        public string UploadedByUserName { get; set; } = "";
        public IFormFile File { get; set; } = null!;
    }

    public class TagDto
    {
        public int TagId { get; set; }
        public List<int> TagValuesIds { get; set; } = new List<int>();  // List of tag values
    }
}
