using System.ComponentModel.DataAnnotations;

namespace ITN2163_AS_Assignment2.Model
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }
        public string UserId { get; set; } 
        public string Activity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string IPAddress { get; set; }
    }
}
