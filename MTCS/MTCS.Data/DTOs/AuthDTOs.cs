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

    public class CreateDriverDTO
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

    public class ProfileDTO
    {
        [MaxLength(25)]
        public string? FullName { get; set; }
        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        public string? PhoneNumber { get; set; }
        public string? CurrentPassword { get; set; }
    }

    public class ProfileResponseDTO
    {
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public DateTime? ModifiedDate { get; set; }
    }
}
