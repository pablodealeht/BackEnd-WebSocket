using Microsoft.AspNetCore.Identity;

namespace BackEnd_WebSocket.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string NombreCompleto { get; set; } = string.Empty;
    }
}
