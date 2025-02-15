public class OverviewDto
{
    public int InsuredPatients { get; set; }
    public int NonInsuredPatients { get; set; }
    public int SaudiPatients { get; set; }
    public int NonSaudiPatients { get; set; }
    public List<AnalyticsDataPoint> PatientAnalytics { get; set; } = new List<AnalyticsDataPoint>();
    public List<ProjectPatientAnalyticsDto> ProjectsPerPatientAnalytics { get; set; } = new List<ProjectPatientAnalyticsDto>();
    public List<InsuranceCompanyPatientAnalyticsDto> InsuranceCompaniesPertPatientAnalytics { get; set; } = new List<InsuranceCompanyPatientAnalyticsDto>();
}

public class ProjectPatientAnalyticsDto
{
    public string ProjectName { get; set; } = "";
    public int TotalPatients { get; set; }
    public int InsuredPatients { get; set; }
    public int NonInsuredPatients { get; set; }
    public double PercentageOfPatientsPerProject { get; set; }
}

public class InsuranceCompanyPatientAnalyticsDto
{
    public string InsuranceCompany { get; set; } = "";
    public int InsuredPatients { get; set; }
    public double PercentageOfPatients { get; set; }
}

public class AnalyticsDataPoint
{
    public string TimeLabel { get; set; } = "";
    public int Count { get; set; }
}
