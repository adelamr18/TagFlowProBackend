public class ProjectAnalyticsDto
{
    public string ProjectName { get; set; } = "";
    public string TimeLabel { get; set; } = DateTime.Now.ToString("");
    public int TotalPatients { get; set; }
    public int InsuredPatients { get; set; }
    public int NonInsuredPatients { get; set; }
}
