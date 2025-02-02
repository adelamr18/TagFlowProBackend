namespace TagFlowApi.Dtos
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public string UpdatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
    }
}
