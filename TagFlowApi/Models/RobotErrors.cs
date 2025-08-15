using System;

namespace TagFlowApi.Models
{
    public class RobotErrors
    {
        public int Id { get; set; }

        public string Module { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public int? FileId { get; set; }
        public File? File { get; set; }

        public string FileName { get; set; } = "";
        public string PatientId { get; set; } = "";
    }
}
