using System.ComponentModel.DataAnnotations;

namespace MTCS.Data.DTOs
{
    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }

    public class RegisterUserDTO
    {
        [Required]
        [MaxLength(25)]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        [Phone]
        public required string PhoneNumber { get; set; }
    }

}
