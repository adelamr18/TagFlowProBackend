public class ProjectTimeKey
{
    public string ProjectName { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Week { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not ProjectTimeKey other)
            return false;
        return ProjectName == other.ProjectName &&
               Nullable.Equals(Date, other.Date) &&
               Year == other.Year &&
               Month == other.Month &&
               Week == other.Week;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ProjectName, Date, Year, Month, Week);
    }
}
