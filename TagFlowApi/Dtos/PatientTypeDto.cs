namespace TagFlowApi.Dtos
{
    public class PatientTypeDto
    {
        public int PatientTypeId { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedByAdminEmail { get; set; } = "";
    }
}
