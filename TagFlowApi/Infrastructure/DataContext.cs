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
        public DbSet<Project> Projects { get; set; }
        public DbSet<PatientType> PatientTypes { get; set; }
        public DbSet<UserProjectPermission> UserProjectPermissions { get; set; }

        public DbSet<ExpiredSsnIds> ExpiredSsnIds { get; set; }

        public DbSet<RobotErrors> RobotErrors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FileRow configuration
            modelBuilder.Entity<FileRow>()
                .HasKey(fr => fr.FileRowId);
            modelBuilder.Entity<FileRow>()
                .HasIndex(fr => fr.SsnId)
                .IsUnique(false);

            modelBuilder.Entity<FileRow>()
                .Property(fr => fr.ProcessingStartedAt)
                .HasColumnType("timestamptz")
                .IsRequired(false);

            modelBuilder.Entity<FileRow>()
                .HasIndex(fr => new { fr.Status, fr.ProcessingStartedAt })
                .IsUnique(false);

            // User and Admin indices
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email)
                .IsUnique();
            modelBuilder.Entity<Admin>()
                .Property(a => a.RoleId)
                .HasDefaultValue(1);

            // Role relationships remain with NoAction (for Roles)
            modelBuilder.Entity<Role>()
                .HasOne(r => r.CreatedByAdmin)
                .WithMany(a => a.Roles)
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Role>()
                .Property(r => r.CreatedBy)
                .IsRequired(false);

            // User-to-Role relationship uses NoAction
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.NoAction);

            // Other User relationships use Restrict
            modelBuilder.Entity<User>()
                .HasOne(u => u.CreatedByAdmin)
                .WithMany(a => a.Users)
                .HasForeignKey(u => u.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedBy)
                .IsRequired(false);

            // Tag relationships use Restrict
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.CreatedByAdmin)
                .WithMany(a => a.Tags)
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Tag>()
                .Property(t => t.CreatedBy)
                .IsRequired(false);

            // TagValue relationships use Restrict
            modelBuilder.Entity<TagValue>()
                .HasOne(tv => tv.Tag)
                .WithMany(t => t.TagValues)
                .HasForeignKey(tv => tv.TagId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TagValue>()
                .HasOne(tv => tv.CreatedByAdmin)
                .WithMany(a => a.TagValues)
                .HasForeignKey(tv => tv.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpiredSsnIds>(entity =>
          {
              entity.HasKey(e => e.ExpiredSsnId);
              entity.Property(e => e.SsnId).IsRequired();
              entity.Property(e => e.FileRowInsuranceExpiryDate).HasColumnType("text");
              entity.Property(e => e.ExpiredAt).HasColumnType("text");
              entity.Property(e => e.FileId).IsRequired();
          });

            // FileTag relationships use Restrict
            modelBuilder.Entity<FileTag>()
                .HasOne(ft => ft.File)
                .WithMany(f => f.FileTags)
                .HasForeignKey(ft => ft.FileId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FileTag>()
                .HasOne(ft => ft.Tag)
                .WithMany(t => t.FileTags)
                .HasForeignKey(ft => ft.TagId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<FileTag>()
                .HasIndex(ft => new { ft.FileId, ft.TagId })
                .IsUnique();

            // UserTagPermission relationships use Restrict
            modelBuilder.Entity<UserTagPermission>()
                .HasKey(utp => new { utp.UserId, utp.TagId });
            modelBuilder.Entity<UserTagPermission>()
                .HasOne(utp => utp.User)
                .WithMany(u => u.UserTagPermissions)
                .HasForeignKey(utp => utp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserTagPermission>()
                .HasOne(utp => utp.Tag)
                .WithMany(t => t.UserTagPermissions)
                .HasForeignKey(utp => utp.TagId)
                .OnDelete(DeleteBehavior.Restrict);

            // FileRow to File relationship uses Restrict
            modelBuilder.Entity<FileRow>()
                .HasOne(fr => fr.File)
                .WithMany(f => f.FileRows)
                .HasForeignKey(fr => fr.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Admin configuration
            modelBuilder.Entity<Admin>()
                .Property(a => a.CreatedAt)
                .HasColumnType("timestamptz");

            // Project configuration
            modelBuilder.Entity<Project>()
                .HasKey(p => p.ProjectId);
            modelBuilder.Entity<Project>()
                .Property(p => p.ProjectName)
                .IsRequired();
            modelBuilder.Entity<Project>()
                .HasOne(p => p.CreatedByAdmin)
                .WithMany(a => a.Projects)
                .HasForeignKey(p => p.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // PatientType configuration
            modelBuilder.Entity<PatientType>()
                .HasKey(pt => pt.PatientTypeId);
            modelBuilder.Entity<PatientType>()
                .Property(pt => pt.Name)
                .IsRequired();
            modelBuilder.Entity<PatientType>()
                .HasIndex(pt => pt.Name)
                .IsUnique();

            // UserProjectPermission configuration uses Restrict
            modelBuilder.Entity<UserProjectPermission>()
                .HasKey(upp => upp.Id);
            modelBuilder.Entity<UserProjectPermission>()
                .HasOne(upp => upp.User)
                .WithMany(u => u.UserProjectPermissions)
                .HasForeignKey(upp => upp.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<UserProjectPermission>()
                .HasOne(upp => upp.Project)
                .WithMany(p => p.UserProjectPermissions)
                .HasForeignKey(upp => upp.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many-to-many relationships for Files
            modelBuilder.Entity<Models.File>()
                .HasMany(f => f.Projects)
                .WithMany(p => p.Files)
                .UsingEntity(j => j.ToTable("FileProjects"));
            modelBuilder.Entity<Models.File>()
                .HasMany(f => f.PatientTypes)
                .WithMany(pt => pt.Files)
                .UsingEntity(j => j.ToTable("FilePatientTypes"));

            // Ensure Role.CreatedBy is nullable
            modelBuilder.Entity<Role>()
                .Property(r => r.CreatedBy)
                .IsRequired(false);

            modelBuilder.Entity<Models.File>()
          .HasOne(f => f.UploadedAdmin)
          .WithMany(a => a.Files)
          .HasForeignKey(f => f.AdminId)
          .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RobotErrors>(entity =>
          {
              entity.HasKey(e => e.Id);

              entity.Property(e => e.Module)
                  .IsRequired()
                  .HasMaxLength(255);

              entity.Property(e => e.ErrorMessage)
                  .IsRequired();

              entity.Property(e => e.Timestamp)
                  .HasColumnType("timestamptz");

              entity.Property(e => e.FileName)
                  .HasMaxLength(500);

              entity.Property(e => e.PatientId)
                  .HasMaxLength(255);

              entity.HasOne(e => e.File)
                  .WithMany(f => f.RobotErrors)
                  .HasForeignKey(e => e.FileId)
                  .OnDelete(DeleteBehavior.SetNull);
          });



        }
    }
}
