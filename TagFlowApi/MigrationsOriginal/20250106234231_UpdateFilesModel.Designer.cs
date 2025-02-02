﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TagFlowApi.Infrastructure;

#nullable disable

namespace TagFlowApi.Migrations
{
    [DbContext(typeof(DataContext))]
    [Migration("20250106234231_UpdateFilesModel")]
    partial class UpdateFilesModel
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("TagFlowApi.Models.Admin", b =>
                {
                    b.Property<int>("AdminId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("AdminId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("CreatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<bool>("IsDeleted")
                        .HasColumnType("bit");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(1);

                    b.Property<string>("UpdatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("AdminId");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Admins");
                });

            modelBuilder.Entity("TagFlowApi.Models.File", b =>
                {
                    b.Property<int>("FileId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("FileId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("FileRowsCounts")
                        .HasColumnType("int");

                    b.Property<string>("FileStatus")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int?>("TagValue")
                        .HasColumnType("int");

                    b.PrimitiveCollection<string>("TagValuesIds")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("UploadedBy")
                        .HasColumnType("int");

                    b.HasKey("FileId");

                    b.HasIndex("UploadedBy");

                    b.ToTable("Files");
                });

            modelBuilder.Entity("TagFlowApi.Models.FileRow", b =>
                {
                    b.Property<int>("RowId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RowId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<string>("Data")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("FileId")
                        .HasColumnType("int");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.Property<int>("TagValueId")
                        .HasColumnType("int");

                    b.Property<string>("TagValuesJson")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("RowId");

                    b.HasIndex("FileId");

                    b.HasIndex("TagId");

                    b.HasIndex("TagValueId");

                    b.ToTable("FileRows");
                });

            modelBuilder.Entity("TagFlowApi.Models.Role", b =>
                {
                    b.Property<int>("RoleId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("RoleId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int");

                    b.Property<string>("RoleName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UpdatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("RoleId");

                    b.HasIndex("CreatedBy");

                    b.ToTable("Roles");
                });

            modelBuilder.Entity("TagFlowApi.Models.Tag", b =>
                {
                    b.Property<int>("TagId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("TagId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("TagName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UpdatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("TagId");

                    b.HasIndex("CreatedBy");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("TagFlowApi.Models.TagValue", b =>
                {
                    b.Property<int>("TagValueId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("TagValueId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("TagValueId");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("TagId");

                    b.ToTable("TagValues");
                });

            modelBuilder.Entity("TagFlowApi.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("UserId"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("datetime2");

                    b.Property<int>("CreatedBy")
                        .HasColumnType("int");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("RoleId")
                        .HasColumnType("int");

                    b.Property<string>("UpdatedBy")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("UserId");

                    b.HasIndex("CreatedBy");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("RoleId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("TagFlowApi.Models.UserTagPermission", b =>
                {
                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.Property<int>("TagId")
                        .HasColumnType("int");

                    b.Property<int>("Id")
                        .HasColumnType("int");

                    b.HasKey("UserId", "TagId");

                    b.HasIndex("TagId");

                    b.ToTable("UserTagPermissions");
                });

            modelBuilder.Entity("TagFlowApi.Models.File", b =>
                {
                    b.HasOne("TagFlowApi.Models.User", "UploadedByUser")
                        .WithMany("Files")
                        .HasForeignKey("UploadedBy")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("UploadedByUser");
                });

            modelBuilder.Entity("TagFlowApi.Models.FileRow", b =>
                {
                    b.HasOne("TagFlowApi.Models.File", "File")
                        .WithMany("FileRows")
                        .HasForeignKey("FileId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.HasOne("TagFlowApi.Models.Tag", "Tag")
                        .WithMany("FileRows")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.HasOne("TagFlowApi.Models.TagValue", "TagValue")
                        .WithMany("FileRows")
                        .HasForeignKey("TagValueId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("File");

                    b.Navigation("Tag");

                    b.Navigation("TagValue");
                });

            modelBuilder.Entity("TagFlowApi.Models.Role", b =>
                {
                    b.HasOne("TagFlowApi.Models.Admin", "CreatedByAdmin")
                        .WithMany("Roles")
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("CreatedByAdmin");
                });

            modelBuilder.Entity("TagFlowApi.Models.Tag", b =>
                {
                    b.HasOne("TagFlowApi.Models.Admin", "CreatedByAdmin")
                        .WithMany("Tags")
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("CreatedByAdmin");
                });

            modelBuilder.Entity("TagFlowApi.Models.TagValue", b =>
                {
                    b.HasOne("TagFlowApi.Models.Admin", "CreatedByAdmin")
                        .WithMany("TagValues")
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.HasOne("TagFlowApi.Models.Tag", "Tag")
                        .WithMany("TagValues")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("CreatedByAdmin");

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("TagFlowApi.Models.User", b =>
                {
                    b.HasOne("TagFlowApi.Models.Admin", "CreatedByAdmin")
                        .WithMany("Users")
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.HasOne("TagFlowApi.Models.Role", "Role")
                        .WithMany("Users")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("CreatedByAdmin");

                    b.Navigation("Role");
                });

            modelBuilder.Entity("TagFlowApi.Models.UserTagPermission", b =>
                {
                    b.HasOne("TagFlowApi.Models.Tag", "Tag")
                        .WithMany("UserTagPermissions")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.HasOne("TagFlowApi.Models.User", "User")
                        .WithMany("UserTagPermissions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.SetNull)
                        .IsRequired();

                    b.Navigation("Tag");

                    b.Navigation("User");
                });

            modelBuilder.Entity("TagFlowApi.Models.Admin", b =>
                {
                    b.Navigation("Roles");

                    b.Navigation("TagValues");

                    b.Navigation("Tags");

                    b.Navigation("Users");
                });

            modelBuilder.Entity("TagFlowApi.Models.File", b =>
                {
                    b.Navigation("FileRows");
                });

            modelBuilder.Entity("TagFlowApi.Models.Role", b =>
                {
                    b.Navigation("Users");
                });

            modelBuilder.Entity("TagFlowApi.Models.Tag", b =>
                {
                    b.Navigation("FileRows");

                    b.Navigation("TagValues");

                    b.Navigation("UserTagPermissions");
                });

            modelBuilder.Entity("TagFlowApi.Models.TagValue", b =>
                {
                    b.Navigation("FileRows");
                });

            modelBuilder.Entity("TagFlowApi.Models.User", b =>
                {
                    b.Navigation("Files");

                    b.Navigation("UserTagPermissions");
                });
#pragma warning restore 612, 618
        }
    }
}
