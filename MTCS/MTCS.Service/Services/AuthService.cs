using Microsoft.Extensions.Configuration;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
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

        public async Task<ApiResponse<string>> RegisterStaff(RegisterUserDTO userDto)
        {
            var contactValidation = await _unitOfWork.ContactHelper.ValidateContact(
        userDto.Email,
        userDto.PhoneNumber);

            if (!contactValidation.Success)
            {
                return new ApiResponse<string>(
                    false,
                    null,
                    contactValidation.Message,
                    contactValidation.MessageVN,
                    null);
            }

            var internalUser = new InternalUser
            {
                UserId = Guid.NewGuid().ToString(),
                FullName = userDto.FullName,
                Email = userDto.Email,
                Password = _passwordHasher.HashPassword(userDto.Password),
                PhoneNumber = userDto.PhoneNumber,
                Role = (int)InternalUserRole.Staff,
                Gender = userDto.Gender.ToString(),
                Birthday = userDto.BirthDate,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.InternalUserRepository.CreateAsync(internalUser);
            return new ApiResponse<string>(true, userDto.FullName, "Registration successful", null, null);
        }

        public async Task<ApiResponse<string>> RegisterAdmin(RegisterUserDTO userDto)
        {
            var contactValidation = await _unitOfWork.ContactHelper.ValidateContact(
        userDto.Email,
        userDto.PhoneNumber);

            if (!contactValidation.Success)
            {
                return new ApiResponse<string>(
                    false,
                    null,
                    contactValidation.Message,
                    contactValidation.MessageVN,
                    null);
            }

            var internalUser = new InternalUser
            {
                UserId = Guid.NewGuid().ToString(),
                FullName = userDto.FullName,
                Email = userDto.Email,
                Password = _passwordHasher.HashPassword(userDto.Password),
                PhoneNumber = userDto.PhoneNumber,
                Role = (int)InternalUserRole.Admin,
                Gender = userDto.Gender.ToString(),
                Birthday = userDto.BirthDate,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.InternalUserRepository.CreateAsync(internalUser);
            return new ApiResponse<string>(true, userDto.FullName, "Registration successful", null, null);
        }

        public async Task<ApiResponse<TokenDTO>> LoginInternalUser(LoginRequestDTO loginDto)
        {
            var user = await _unitOfWork.InternalUserRepository.GetUserByEmailAsync(loginDto.Email);

            if (user == null || user.DeletedBy != null)
            {
                throw new KeyNotFoundException("User not found");
            }

            if (!_passwordHasher.VerifyPassword(loginDto.Password, user.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            var token = await _tokenService.GenerateTokensForInternalUser(user);
            return new ApiResponse<TokenDTO>(true, token, "Login successful", "Login thành công", null);
        }

        public async Task<ApiResponse<ProfileResponseDTO>> GetUserProfile(string userId)
        {
            var profile = await _unitOfWork.InternalUserRepository.GetUserProfile(userId);
            if (profile == null)
            {
                return new ApiResponse<ProfileResponseDTO>(false, null, "User not found", "Không tìm thấy người dùng", null);
            }

            return new ApiResponse<ProfileResponseDTO>(true, profile, "Profile retrieved successfully", "Lấy thông tin hồ sơ thành công", null);
        }

        public async Task<ApiResponse<ProfileResponseDTO>> UpdateInternalUserProfile(string userId, ProfileDTO profileDto)
        {
            var user = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found");
            }

            bool emailChanged = profileDto.Email != null && profileDto.Email != user.Email;
            bool phoneChanged = profileDto.PhoneNumber != null && profileDto.PhoneNumber != user.PhoneNumber;

            if (emailChanged)
            {
                if (string.IsNullOrEmpty(profileDto.CurrentPassword))
                {
                    return new ApiResponse<ProfileResponseDTO>(false, null, "Password required",
                        "Mật khẩu hiện tại là bắt buộc để cập nhật email",
                        "Current password is required to update email");
                }

                if (!_passwordHasher.VerifyPassword(profileDto.CurrentPassword, user.Password))
                {
                    return new ApiResponse<ProfileResponseDTO>(false, null, "Invalid password", "Mật khẩu hiện tại không đúng",
                        "Current password is incorrect");
                }
            }

            if (emailChanged || phoneChanged)
            {
                string emailToCheck = emailChanged ? profileDto.Email : user.Email;
                string phoneToCheck = phoneChanged ? profileDto.PhoneNumber : user.PhoneNumber;

                var contactValidation = await _unitOfWork.ContactHelper.ValidateContact(emailToCheck, phoneToCheck, userId);

                if (!contactValidation.Success)
                {
                    return new ApiResponse<ProfileResponseDTO>(
                        false,
                        null,
                        contactValidation.Message,
                        contactValidation.MessageVN,
                        null);
                }
            }
            user.FullName = profileDto.FullName ?? user.FullName;
            if (emailChanged) user.Email = profileDto.Email;
            if (phoneChanged) user.PhoneNumber = profileDto.PhoneNumber;
            user.Gender = profileDto.Gender;
            user.Birthday = profileDto.Birthday;
            user.ModifiedDate = DateTime.Now;
            user.ModifiedBy = userId;

            await _unitOfWork.InternalUserRepository.UpdateAsync(user);

            var newProfile = new ProfileResponseDTO
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                ModifiedDate = user.ModifiedDate
            };

            return new ApiResponse<ProfileResponseDTO>(true, newProfile, "Updated profile successfully", "Cập nhật hồ sơ thành công", null);
        }

        public async Task<ApiResponse<TokenDTO>> LoginDriver(LoginRequestDTO loginDto)
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

            var token = await _tokenService.GenerateTokensForDriver(driver);

            return new ApiResponse<TokenDTO>(true, token, "Login successful", "Đăng nhập thành công", null);
        }
    }
}
