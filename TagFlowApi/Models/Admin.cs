// TagFlowApi/Models/Admin.cs
namespace TagFlowApi.Models
{
    public class Admin
    {
        public int AdminId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
        public ICollection<Role> Roles { get; set; } = [];
        public ICollection<User> Users { get; set; } = [];
        public ICollection<Tag> Tags { get; set; } = [];
        public ICollection<TagValue> TagValues { get; set; } = [];
        public string UpdatedBy { get; set; } = "";
        public bool IsDeleted { get; set; } = false;
        public int RoleId { get; set; } = 1;

        public bool CheckPassword(string password)
        {
            var hashedPassword = Utils.Helpers.HashPassword(password);
            return hashedPassword == PasswordHash;
        }
    }
}
