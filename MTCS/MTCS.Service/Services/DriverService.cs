using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class DriverService : IDriverService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IPasswordHasher _passwordHasher;

        public DriverService(UnitOfWork unitOfWork, IPasswordHasher passwordHasher)
        {
            _unitOfWork = unitOfWork;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponse<PagedList<ViewDriverDTO>>> ViewDrivers(PaginationParams paginationParams, int? status = null)
        {
            var pagedDrivers = await _unitOfWork.DriverRepository.GetDrivers(
                paginationParams, status);

            string message = status.HasValue
                ? $"Drivers with status {(UserStatus)status.Value} retrieved successfully"
                : "All drivers retrieved successfully";

            return new ApiResponse<PagedList<ViewDriverDTO>>(
                true,
                pagedDrivers,
                message,
                null);
        }

        public async Task<ApiResponse<string>> CreateDriver(CreateDriverDTO driverDto)
        {
            if (await _unitOfWork.DriverRepository.EmailExistsAsync(driverDto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }

            var driver = new Driver
            {
                DriverId = Guid.NewGuid().ToString(),
                FullName = driverDto.FullName,
                Email = driverDto.Email,
                Password = _passwordHasher.HashPassword(driverDto.Password),
                PhoneNumber = driverDto.PhoneNumber,
                Status = (int)UserStatus.Active,
                CreatedDate = DateTime.Now
            };


            await _unitOfWork.DriverRepository.CreateAsync(driver);

            string successMessage = $"Driver {driverDto.FullName} registered successfully";
            return new ApiResponse<string>(true, successMessage, "Registration successful", null);
        }

        public async Task<ApiResponse<DriverProfileDetailsDTO>> GetDriverProfile(string driverId)
        {
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(driverId);
            if (driver == null)
            {
                throw new InvalidOperationException("Driver not found");
            }

            var (totalWorkingTime, currentWeekWorkingTime, fileUrls) =
                await _unitOfWork.DriverRepository.GetDriverProfileDetails(driverId);

            var driverProfileDetails = new DriverProfileDetailsDTO
            {
                DriverId = driver.DriverId,
                FullName = driver.FullName,
                Email = driver.Email,
                DateOfBirth = driver.DateOfBirth,
                PhoneNumber = driver.PhoneNumber,
                Status = driver.Status,
                CreatedDate = driver.CreatedDate,
                CreatedBy = driver.CreatedBy,
                ModifiedDate = driver.ModifiedDate,
                ModifiedBy = driver.ModifiedBy,

                TotalWorkingTime = totalWorkingTime,
                CurrentWeekWorkingTime = currentWeekWorkingTime,

                FileUrls = fileUrls
            };

            return new ApiResponse<DriverProfileDetailsDTO>(
                true,
                driverProfileDetails,
                "Driver profile details retrieved successfully",
                null);
        }
    }
}
