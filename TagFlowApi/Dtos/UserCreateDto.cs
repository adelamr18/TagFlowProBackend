namespace TagFlowApi.Dtos
{
    public class UserCreateDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RoleId { get; set; } = 0;
        public List<int> AssignedTagIds { get; set; } = [];
        public string CreatedByAdminEmail { get; set; } = string.Empty;

    }
}
