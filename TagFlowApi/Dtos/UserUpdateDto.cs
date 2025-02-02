namespace TagFlowApi.Dtos
{
    public class UserUpdateDto
    {
        public string? Username { get; set; } = "";
        public int? RoleId { get; set; } = null;
        public List<int>? AssignedTagIds { get; set; } = [];
        public string UpdatedBy { get; set; } = "";
    }
}

