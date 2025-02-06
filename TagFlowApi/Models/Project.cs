using System;
using System.Collections.Generic;

namespace TagFlowApi.Models
{
    public class Project
    {
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional: Admin who created the project
        public int? CreatedBy { get; set; }
        public Admin? CreatedByAdmin { get; set; }

        // Navigation: Permissions assigned to users for this project
        public ICollection<UserProjectPermission> UserProjectPermissions { get; set; } = new List<UserProjectPermission>();
        public string UpdatedBy { get; set; } = "";
        public ICollection<File> Files { get; set; } = new List<File>();
    }
}
