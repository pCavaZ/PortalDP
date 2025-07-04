using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Application.DTOs
{
    public class LoginDto
    {
        public string DNI { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public StudentDto? Student { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

}
