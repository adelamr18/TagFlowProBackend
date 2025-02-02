using TagFlowApi.Models;

namespace TagFlowApi.Dtos
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CreatedByAdminName { get; set; } = string.Empty;
        public string CreatedByAdminEmail { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public ICollection<UserTagPermission> UserTagPermissions { get; set; } = new List<UserTagPermission>();
        public ICollection<Models.File> Files { get; set; } = new List<Models.File>();
        public int CreatedBy { get; set; } = 0;
        public int RoleId { get; set; }
        public List<string> AssignedTags { get; set; } = [];
        public string UpdatedBy { get; set; } = string.Empty;

    }
}
