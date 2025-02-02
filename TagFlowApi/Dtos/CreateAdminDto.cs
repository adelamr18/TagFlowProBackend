
namespace TagFlowApi.Dtos
{
    public class CreateAdminDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
