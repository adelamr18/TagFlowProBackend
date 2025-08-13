namespace TagFlowApi.Models
{
    public class UserProjectPermission
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public int? UserId { get; set; }

        public User? User { get; set; }
        public Project? Project { get; set; }
    }
}
