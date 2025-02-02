// TagFlowApi/Models/Role.cs
namespace TagFlowApi.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public Admin? CreatedByAdmin { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
        public string UpdatedBy { get; set; } = "";
    }
}
