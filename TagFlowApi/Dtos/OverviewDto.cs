public class OverviewDto
{
    public int InsuredPatients { get; set; }
    public int NonInsuredPatients { get; set; }
    public int SaudiPatients { get; set; }
    public int NonSaudiPatients { get; set; }
    public Dictionary<string, int> TotalPatientsPerProjectOverview { get; set; } = [];
}
