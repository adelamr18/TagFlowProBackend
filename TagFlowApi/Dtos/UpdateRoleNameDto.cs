namespace TagFlowApi.Dtos
{
    public class UpdateRoleNameDto
    {
        public int RoleId { get; set; }
        public string NewRoleName { get; set; } = "";
        public string UpdatedBy { get; set; } = "";
    }
}
