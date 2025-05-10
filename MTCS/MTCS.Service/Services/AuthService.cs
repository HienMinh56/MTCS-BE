using Microsoft.Extensions.Configuration;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Base;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthService(UnitOfWork unitOfWork, ITokenService tokenService, IPasswordHasher passwordHasher, IConfiguration configuration, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _emailService = emailService;
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
            var staffId = await _unitOfWork.InternalUserRepository.GenerateStaffIdAsync();

            var internalUser = new InternalUser
            {
                UserId = staffId,
                FullName = userDto.FullName,
                Email = userDto.Email,
                Password = _passwordHasher.HashPassword(userDto.Password),
                PhoneNumber = userDto.PhoneNumber,
                Role = (int)InternalUserRole.Staff,
                Gender = userDto.Gender.ToString(),
                Status = (int)UserStatus.Active,
                Birthday = userDto.BirthDate,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.InternalUserRepository.CreateAsync(internalUser);

            string subject = "Thông báo tạo tài khoản nhân viên mới";

            await _emailService.SendAccountRegistration(
                userDto.Email,
                subject,
                userDto.FullName,
                userDto.Email,
                userDto.Password
            );

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
            var adminId = await _unitOfWork.InternalUserRepository.GenerateAdminIdAsync();

            var internalUser = new InternalUser
            {
                UserId = adminId,
                FullName = userDto.FullName,
                Email = userDto.Email,
                Password = _passwordHasher.HashPassword(userDto.Password),
                PhoneNumber = userDto.PhoneNumber,
                Role = (int)InternalUserRole.Admin,
                Gender = userDto.Gender.ToString(),
                Status = (int)UserStatus.Active,
                Birthday = userDto.BirthDate,
                CreatedDate = DateTime.Now
            };

            await _unitOfWork.InternalUserRepository.CreateAsync(internalUser);

            string subject = "Thông báo tạo tài khoản quản trị viên mới";
            await _emailService.SendAccountRegistration(
                userDto.Email,
                subject,
                userDto.FullName,
                userDto.Email,
                userDto.Password
            );

            return new ApiResponse<string>(true, userDto.FullName, "Registration successful", null, null);
        }

        public async Task<ApiResponse<TokenDTO>> LoginInternalUser(LoginRequestDTO loginDto)
        {
            var user = await _unitOfWork.InternalUserRepository.GetUserByEmailAsync(loginDto.Email);

            if (user == null || user.Status == (int)UserStatus.Inactive)
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

            if (driver == null || driver.Status == (int)UserStatus.Inactive)
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

        public async Task<ApiResponse<PagedList<InternalUser>>> GetInternalUserWithFilter(
            PaginationParams paginationParams,
            string? keyword = null,
            int? role = null)
        {
            var pagedUsers = await _unitOfWork.InternalUserRepository.GetInternalUserWithFilter(
                paginationParams,
                keyword,
                role);

            return new ApiResponse<PagedList<InternalUser>>(
                true,
                pagedUsers,
                "Users retrieved successfully",
                "Lấy danh sách người dùng thành công",
                null);
        }

        public async Task<BusinessResult> GetStaffForDriver()
        {
            try
            {
                var staff = _unitOfWork.InternalUserRepository.GetList(i => i.Role == (int)InternalUserRole.Staff && i.Status == (int)UserStatus.Active);
                return new BusinessResult { Status = Const.SUCCESS_READ_CODE, Message = Const.SUCCESS_READ_MSG, Data = staff };
            }
            catch (Exception ex)
            {
                return new BusinessResult { Status = Const.FAIL_READ_CODE, Message = ex.Message };
            }
        }
        public async Task<ApiResponse<string>> ChangeUserActivationStatus(string userId, int newStatus, string modifierId)
        {
            var user = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<string>(false, null, "User not found", "Không tìm thấy người dùng", null);
            }

            if (user.Status == newStatus)
            {
                string message = newStatus == (int)UserStatus.Active
                    ? "User is already active"
                    : "User is already inactive";
                string messageVN = newStatus == (int)UserStatus.Active
                    ? "Người dùng đã được kích hoạt"
                    : "Người dùng đã bị vô hiệu hóa";
                return new ApiResponse<string>(false, null, message, messageVN, null);
            }

            if (newStatus == (int)UserStatus.Active)
            {
                user.DeletedDate = null;
                user.DeletedBy = null;
                user.Status = (int)UserStatus.Active;
                user.ModifiedDate = DateTime.Now;
                user.ModifiedBy = modifierId;
            }
            else
            {
                user.DeletedDate = DateTime.Now;
                user.DeletedBy = modifierId;
                user.Status = (int)UserStatus.Inactive;
            }

            await _unitOfWork.InternalUserRepository.UpdateAsync(user);

            string successMessage = newStatus == (int)UserStatus.Active
                ? "User activated successfully"
                : "User deactivated successfully";
            string successMessageVN = newStatus == (int)UserStatus.Active
                ? "Kích hoạt người dùng thành công"
                : "Vô hiệu hóa người dùng thành công";

            return new ApiResponse<string>(
                true,
                user.FullName,
                successMessage,
                successMessageVN,
                null);
        }

        public async Task<ApiResponse<ProfileResponseDTO>> UpdateUserInformation(string userId, AdminUpdateUserDTO updateDto, string modifierId)
        {
            var user = await _unitOfWork.InternalUserRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse<ProfileResponseDTO>(false, null, "User not found", "Không tìm thấy người dùng", null);
            }

            bool emailChanged = updateDto.Email != null && updateDto.Email != user.Email;
            bool phoneChanged = updateDto.PhoneNumber != null && updateDto.PhoneNumber != user.PhoneNumber;

            if (emailChanged || phoneChanged)
            {
                string emailToCheck = emailChanged ? updateDto.Email : user.Email;
                string phoneToCheck = phoneChanged ? updateDto.PhoneNumber : user.PhoneNumber;

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

            user.FullName = updateDto.FullName ?? user.FullName;
            if (emailChanged) user.Email = updateDto.Email;
            if (phoneChanged) user.PhoneNumber = updateDto.PhoneNumber;
            user.Gender = updateDto.Gender ?? user.Gender;
            user.Birthday = updateDto.Birthday ?? user.Birthday;

            if (!string.IsNullOrEmpty(updateDto.NewPassword))
            {
                user.Password = _passwordHasher.HashPassword(updateDto.NewPassword);
            }

            user.ModifiedDate = DateTime.Now;
            user.ModifiedBy = modifierId;

            await _unitOfWork.InternalUserRepository.UpdateAsync(user);

            var updatedProfile = new ProfileResponseDTO
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                Birthday = user.Birthday,
                ModifiedDate = user.ModifiedDate
            };

            return new ApiResponse<ProfileResponseDTO>(
                true,
                updatedProfile,
                "Updated user profile successfully",
                "Cập nhật hồ sơ người dùng thành công",
                null);
        }

    }
}
