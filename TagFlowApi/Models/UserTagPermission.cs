// TagFlowApi/Models/UserTagPermission.cs
namespace TagFlowApi.Models
{
    public class UserTagPermission
    {
        public int Id { get; set; }
        public int TagId { get; set; }
        public int UserId { get; set; }


        // Foreign key to User table
        public User User { get; set; } = null!;

        // Foreign key to Tag table
        public Tag Tag { get; set; } = null!;
    }
}
