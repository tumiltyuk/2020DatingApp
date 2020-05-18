using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.Dtos
{
    public class UserForRegisterDto
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [StringLength(14, MinimumLength = 4, ErrorMessage = "You must specify a password between 4 and 14 characters")]
        public string Password { get; set; }
    }
}