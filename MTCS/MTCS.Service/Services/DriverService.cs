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
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IPasswordHasher _passwordHasher;

        public DriverService(UnitOfWork unitOfWork, IPasswordHasher passwordHasher, IFirebaseStorageService firebaseStorageService)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
            _passwordHasher = passwordHasher;
        }

        public async Task<ApiResponse<PagedList<ViewDriverDTO>>> ViewDrivers(PaginationParams paginationParams, int? status = null, string? keyword = null)
        {
            var pagedDrivers = await _unitOfWork.DriverRepository.GetDrivers(
                paginationParams, status, keyword);

            return new ApiResponse<PagedList<ViewDriverDTO>>(
                true,
                pagedDrivers,
                "Get drivers successfully",
                null,
                null);
        }

        public async Task<ApiResponse<DriverResponseDTO>> CreateDriverWithFiles(CreateDriverDTO driverDto, List<FileUploadDTO> fileUploads,
    string userId)
        {
            if (await _unitOfWork.DriverRepository.EmailExistsAsync(driverDto.Email))
            {
                throw new InvalidOperationException("Email already exists");
            }
            var driverId = Guid.NewGuid().ToString();

            var driver = new Driver
            {
                DriverId = driverId,
                FullName = driverDto.FullName,
                Email = driverDto.Email,
                DateOfBirth = driverDto.DateOfBirth,
                Password = _passwordHasher.HashPassword(driverDto.Password),
                PhoneNumber = driverDto.PhoneNumber,
                Status = (int)UserStatus.Active,
                CreatedDate = DateTime.Now
            };


            await _unitOfWork.DriverRepository.CreateAsync(driver);

            var responseDto = new DriverResponseDTO
            {
                DriverId = driver.DriverId,
                FullName = driver.FullName,
                Email = driver.Email,
                PhoneNumber = driver.PhoneNumber,
                Status = driver.Status,
                DateOfBirth = driver.DateOfBirth,
                CreatedDate = driver.CreatedDate
            };

            if (fileUploads != null && fileUploads.Count > 0)
            {
                try
                {
                    foreach (var fileUpload in fileUploads)
                    {
                        if (fileUpload.File != null && fileUpload.File.Length > 0)
                        {
                            var fileMetadata = await _firebaseStorageService.UploadFileAsync(fileUpload.File);

                            var driverFile = new DriverFile
                            {
                                FileId = Guid.NewGuid().ToString(),
                                DriverId = driverId,
                                FileName = fileUpload.File.FileName,
                                FileType = fileMetadata.FileType,
                                FileUrl = fileMetadata.FileUrl,
                                UploadDate = DateTime.Now,
                                UploadBy = userId,
                                Description = fileUpload.Description,
                                Note = fileUpload.Note,
                                ModifiedDate = null,
                                ModifiedBy = null,
                                DeletedDate = null,
                                DeletedBy = null
                            };

                            await _unitOfWork.DriverFileRepository.CreateAsync(driverFile);
                        }
                    }
                    return new ApiResponse<DriverResponseDTO>(
                        true,
                        responseDto,
                        "Tractor and files created successfully",
                        "Đã tạo tài xế và tệp đính kèm thành công",
                        null);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<DriverResponseDTO>(
                        true,
                        responseDto,
                        "Tractor created successfully but there was an issue with file uploads",
                        "Đã tạo tài xế thành công nhưng có vấn đề khi upload file",
                        ex.Message);
                }
            }

            string successMessage = $"Driver {driverDto.FullName} registered successfully";
            return new ApiResponse<DriverResponseDTO>(true, null, successMessage, "Tạo tài xế thành công", null);
        }

        public async Task<ApiResponse<DriverProfileDetailsDTO>> GetDriverProfile(string driverId)
        {
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(driverId);
            if (driver == null)
            {
                throw new InvalidOperationException("Driver not found");
            }

            var (totalWorkingTime, currentWeekWorkingTime, files) =
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
                Files = files
            };

            return new ApiResponse<DriverProfileDetailsDTO>(
                true,
                driverProfileDetails,
                "Driver profile details retrieved successfully",
                null,
                null);
        }
    }
}
