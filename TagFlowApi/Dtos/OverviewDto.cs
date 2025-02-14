public class OverviewDto
{
    public int InsuredPatients { get; set; }
    public int NonInsuredPatients { get; set; }
    public int SaudiPatients { get; set; }
    public int NonSaudiPatients { get; set; }
    public List<AnalyticsDataPoint> PatientAnalytics { get; set; } = [];
    public List<ProjectPatientAnalyticsDto> ProjectsPerPatientAnalytics { get; set; } = [];
}

public class ProjectPatientAnalyticsDto
{
    public string ProjectName { get; set; } = "";
    public int TotalPatients { get; set; }
    public int InsuredPatients { get; set; }
    public int NonInsuredPatients { get; set; }
    public double PercentageOfPatientsPerProject { get; set; }
}

public class AnalyticsDataPoint
{
    public string TimeLabel { get; set; } = "";
    public int Count { get; set; } = 0;
}
