namespace TagFlowApi.DTOs
{
    public class FileRowDto
    {
        public int FileRowId { get; set; }
        public string Status { get; set; } = "Unprocessed";
        public int FileId { get; set; }
        public string Ssn { get; set; } = "";
        public string InsuranceCompany { get; set; } = "";
        public string MedicalNetwork { get; set; } = "";
        public string IdentityNumber { get; set; } = "";
        public string PolicyNumber { get; set; } = "";
        public string Class { get; set; } = "";
        public string DeductIblerate { get; set; } = "";
        public string MaxLimit { get; set; } = "";
        public string UploadDate { get; set; } = "";
        public string InsuranceExpiryDate { get; set; } = "";
        public string BeneficiaryType { get; set; } = "";
        public string BeneficiaryNumber { get; set; } = "";
    }
}
