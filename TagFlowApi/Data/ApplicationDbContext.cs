using Microsoft.EntityFrameworkCore;
using TagFlowApi.Models;
using File = TagFlowApi.Models.File;

namespace TagFlowApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<TagValue> TagValues { get; set; } = null!;
        public DbSet<UserTagPermission> UserTagPermissions { get; set; } = null!;
        public DbSet<File> Files { get; set; } = null!;
        public DbSet<FileTag> FileTags { get; set; } = null!;
        public DbSet<FileRow> FileRows { get; set; } = null!;
    }
}
