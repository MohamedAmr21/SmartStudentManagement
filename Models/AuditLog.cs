using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }
        public string? AuditUserId { get; set; }
        [Required,MaxLength(100)] public string Action { get; set; }=string.Empty;
        [Required,MaxLength(100)] public string EntityType { get; set; }=string.Empty;
        public string? EntityId { get; set; }

        public DateTime Timestamp { get; set; }=DateTime.UtcNow; 

    }
    
}