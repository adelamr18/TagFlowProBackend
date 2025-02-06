namespace TagFlowApi.Dtos
{
    public class ProjectDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public DateTime? CreatedAt { get; set; }
        public string CreatedByAdminEmail { get; set; } = "";
        public List<int> AssignedUserIds { get; set; } = [];

    }
}
