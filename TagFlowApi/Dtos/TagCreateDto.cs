namespace TagFlowApi.Dtos
{
    public class TagCreateDto
    {
        public string TagName { get; set; } = string.Empty;
        public List<string> TagValues { get; set; } = [];
        public List<string> AssignedUsers { get; set; } = [];
        public string AdminUsername { get; set; } = string.Empty;
    }
}

