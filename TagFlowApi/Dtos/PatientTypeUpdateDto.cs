namespace TagFlowApi.Dtos
{
    public class PatientTypeUpdateDto
    {
        public int PatientTypeId { get; set; }
        public string Name { get; set; } = "";
        public string UpdatedBy { get; set; } = "";
    }
}
