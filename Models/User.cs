
using Microsoft.AspNetCore.Identity;

namespace SmartStudentManagement.Models
{
    public class User : IdentityUser
    {
       public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    
}