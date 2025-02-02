using System;
using System.Security.Cryptography;
using System.Text;

namespace TagFlowApi.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        // Foreign key to Role table
        public Role Role { get; set; } = null!;

        public int RoleId { get; set; }
        public int CreatedBy { get; set; }

        // Foreign key to Admin table (creator of the user)
        public Admin? CreatedByAdmin { get; set; }

        // Many-to-many relationship with tags through UserTagPermissions
        public ICollection<UserTagPermission> UserTagPermissions { get; set; } = new List<UserTagPermission>();
        // Many-to-one relationship with files
        public ICollection<File> Files { get; set; } = new List<File>();
        public string UpdatedBy { get; set; } = "";
        public bool CheckPassword(string password)
        {
            var hashedPassword = Utils.Helpers.HashPassword(password);
            return hashedPassword == PasswordHash;
        }

    }
}
