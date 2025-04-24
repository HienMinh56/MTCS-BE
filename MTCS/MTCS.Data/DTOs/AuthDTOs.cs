using System.ComponentModel.DataAnnotations;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;

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

        [Required]
        [Phone]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0")]
        public required string PhoneNumber { get; set; }

        [Required]
        [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender")]
        public Gender Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        [CustomValidation(typeof(DateValidator), nameof(DateValidator.DoB))]
        public DateOnly BirthDate { get; set; }
    }

    public class CreateDriverDTO
    {
        [Required]
        [MaxLength(25)]
        public required string FullName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [Required]
        [MinLength(6)]
        public required string Password { get; set; }

        [Required]
        [Phone]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0")]
        public required string PhoneNumber { get; set; }
    }

    public class ProfileDTO
    {
        [MaxLength(25)]
        public string? FullName { get; set; }
        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0")]
        public string? PhoneNumber { get; set; }
        public DateOnly? Birthday { get; set; }
        public string? Gender { get; set; }
        public string? CurrentPassword { get; set; }
    }

    public class ProfileResponseDTO
    {
        public string UserId { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string Gender { get; set; } = null!;
        public DateOnly? Birthday { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class AdminUpdateUserDTO
    {
        [MaxLength(25)]
        public string? FullName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Phone]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be 10 digits and start with 0")]
        public string? PhoneNumber { get; set; }

        public DateOnly? Birthday { get; set; }

        public string? Gender { get; set; }

        [MinLength(6)]
        public string? NewPassword { get; set; }
    }

}
