using Microsoft.EntityFrameworkCore;
using TagFlowApi.Models;

namespace TagFlowApi.Infrastructure
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TagValue> TagValues { get; set; }
        public DbSet<UserTagPermission> UserTagPermissions { get; set; }
        public DbSet<Models.File> Files { get; set; }
        public DbSet<FileTag> FileTags { get; set; }
        public DbSet<FileRow> FileRows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FileRow>()
          .HasKey(fr => fr.FileRowId);

            modelBuilder.Entity<FileRow>()
                  .HasIndex(fr => fr.SsnId)
                  .IsUnique(false);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email)
                .IsUnique();

            modelBuilder.Entity<Admin>()
                .Property(a => a.RoleId)
                .HasDefaultValue(1);

            modelBuilder.Entity<Role>()
                .HasOne(r => r.CreatedByAdmin)
                .WithMany(a => a.Roles)
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.CreatedByAdmin)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Tag>()
                .HasOne(t => t.CreatedByAdmin)
                .WithMany(a => a.Tags)
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TagValue>()
                .HasOne(tv => tv.Tag)
                .WithMany(t => t.TagValues)
                .HasForeignKey(tv => tv.TagId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TagValue>()
                .HasOne(tv => tv.CreatedByAdmin)
                .WithMany(a => a.TagValues)
                .HasForeignKey(tv => tv.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FileTag>()
                    .HasOne(ft => ft.File)
                    .WithMany(f => f.FileTags)
                    .HasForeignKey(ft => ft.FileId)
                    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FileTag>()
                .HasOne(ft => ft.Tag)
                .WithMany(t => t.FileTags)
                .HasForeignKey(ft => ft.TagId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FileTag>()
                .HasIndex(ft => new { ft.FileId, ft.TagId })
                .IsUnique();

            modelBuilder.Entity<UserTagPermission>()
                .HasKey(utp => new { utp.UserId, utp.TagId });

            modelBuilder.Entity<UserTagPermission>()
                .HasOne(utp => utp.User)
                .WithMany(u => u.UserTagPermissions)
                .HasForeignKey(utp => utp.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserTagPermission>()
                .HasOne(utp => utp.Tag)
                .WithMany(t => t.UserTagPermissions)
                .HasForeignKey(utp => utp.TagId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FileRow>()
                    .HasOne(fr => fr.File)
                    .WithMany(f => f.FileRows)
                    .HasForeignKey(fr => fr.FileId)
                    .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
