namespace TagFlowApi.Models
{
    public class ExpiredSsnIds
    {
        public int ExpiredSsnId { get; set; }
        public int FileRowId { get; set; }
        public string SsnId { get; set; } = "";
        public string FileRowInsuranceExpiryDate { get; set; } = "";
        public string ExpiredAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        public int FileId { get; set; }
        public File File { get; set; } = null!;
    }
}
