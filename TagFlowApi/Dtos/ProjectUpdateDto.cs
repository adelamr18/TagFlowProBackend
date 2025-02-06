namespace TagFlowApi.Dtos
{
    public class ProjectUpdateDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public List<int> AssignedUserIds { get; set; } = new List<int>();
        public string UpdatedBy { get; set; } = "";
    }
}
