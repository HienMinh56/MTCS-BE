using Microsoft.Extensions.Configuration;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(UnitOfWork unitOfWork, ITokenService tokenService, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        public async Task<ApiResponse<string>> RegisterCustomerAsync(RegisterUserDTO userDto)
        {
            if (await _unitOfWork.UserRepository.EmailExistsAsync(userDto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                FullName = userDto.FullName,
                Email = userDto.Email,
                Password = _passwordHasher.HashPassword(userDto.Password),
                PhoneNumber = userDto.PhoneNumber,
                Role = Role.Customer.Name,
                Status = (int)UserStatus.Active,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.UserRepository.CreateAsync(user);

            string successMessage = $"User {userDto.FullName} registered successfully";
            return new ApiResponse<string>(true, successMessage, "Registration successful", null);
        }

        public async Task<ApiResponse<TokenDTO>> LoginUserAsync(LoginRequestDTO loginDto)
        {
            var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(loginDto.Email);

            if (user == null || user.Status != (int)UserStatus.Active)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!_passwordHasher.VerifyPassword(loginDto.Password, user.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var token = await _tokenService.GenerateTokensForUserAsync(user);

            return new ApiResponse<TokenDTO>(true, token, "Đăng nhập thành công", null);
        }

        public async Task<ApiResponse<TokenDTO>> LoginDriverAsync(LoginRequestDTO loginDto)
        {
            var driver = await _unitOfWork.DriverRepository.GetDriverByEmailAsync(loginDto.Email);

            if (driver == null)
            {
                throw new KeyNotFoundException("Không tìm thấy tài xế");
            }

            if (!_passwordHasher.VerifyPassword(loginDto.Password, driver.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var token = await _tokenService.GenerateTokensForDriverAsync(driver);

            return new ApiResponse<TokenDTO>(true, token, "Login successful", null);
        }
    }
}
