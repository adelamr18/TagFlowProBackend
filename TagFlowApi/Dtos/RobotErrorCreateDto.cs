using System;

namespace TagFlowApi.DTOs
{
    public class RobotErrorCreateDto
    {
        public string Module { get; set; } = default!;
        public string ErrorMessage { get; set; } = default!;
        public DateTime? Timestamp { get; set; }   // optional; defaulted to UtcNow if null

        public int? FileId { get; set; }           // optional relation
        public string? FileName { get; set; }
        public string? PatientId { get; set; }
    }
}
