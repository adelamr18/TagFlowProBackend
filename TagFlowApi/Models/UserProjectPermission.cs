namespace TagFlowApi.Models
{
    public class UserProjectPermission
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }  // Make nullable for SET NULL
        public int? UserId { get; set; }     // If you want SET NULL here as well

        public User? User { get; set; }
        public Project? Project { get; set; }
    }
}
