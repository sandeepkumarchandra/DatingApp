using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "UserName is required.")]
        public string Username { get; set; } = string.Empty;
        [MaxLength(8),MinLength(5)]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }=string.Empty;
    }
    
}