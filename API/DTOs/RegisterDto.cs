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
        [Required] public string KnownAs { get; set; }
        [Required] public string Gender { get; set; }
        [Required] public DateTime DateOfBirth { get; set; }
        [Required] public string City { get; set; }
        [Required] public string Country { get; set; }
        
        [MaxLength(8),MinLength(5)]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }=string.Empty;
    }
    
}