namespace TagFlowApi.Models
{
    public class PatientType
    {
        public int PatientTypeId { get; set; }
        public string Name { get; set; } = "";
        public ICollection<File> Files { get; set; } = new List<File>();
        public string CreatedByAdminEmail { get; set; } = "";
    }
}
