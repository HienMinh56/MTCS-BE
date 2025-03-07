using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
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
                CreatedDate = DateTime.UtcNow
            };


            await _unitOfWork.DriverRepository.CreateAsync(driver);

            string successMessage = $"Driver {driverDto.FullName} registered successfully";
            return new ApiResponse<string>(true, successMessage, "Registration successful", null);
        }
    }
}
