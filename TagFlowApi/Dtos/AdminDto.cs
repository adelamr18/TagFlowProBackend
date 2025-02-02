
namespace TagFlowApi.Dtos
{
    public class AdminDto
    {
        public int AdminId { get; set; } = 0;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CreatedByAdminEmail { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public int RoleId { get; set; } = 0;
    }
}
