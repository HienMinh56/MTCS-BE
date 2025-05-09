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
        private readonly IEmailService _emailService;

        public DriverService(UnitOfWork unitOfWork, IPasswordHasher passwordHasher, IFirebaseStorageService firebaseStorageService, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
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
            var contactValidation = await _unitOfWork.ContactHelper.ValidateContact(
         driverDto.Email,
         driverDto.PhoneNumber);

            if (!contactValidation.Success)
            {
                return new ApiResponse<DriverResponseDTO>(
                    false,
                    null,
                    contactValidation.Message,
                    contactValidation.MessageVN,
                    null);
            }
            var driverId = await _unitOfWork.DriverRepository.GenerateDriverIdAsync();

            var driver = new Driver
            {
                DriverId = driverId,
                FullName = driverDto.FullName,
                Email = driverDto.Email,
                DateOfBirth = driverDto.DateOfBirth,
                Password = _passwordHasher.HashPassword(driverDto.Password),
                PhoneNumber = driverDto.PhoneNumber,
                Status = (int)DriverStatus.Active,
                TotalProcessedOrders = 0,
                CreatedDate = DateTime.Now,
                CreatedBy = userId
            };

            await _unitOfWork.DriverRepository.CreateAsync(driver);

            string subject = "Thông báo tạo tài khoản tài xế viên mới";
            await _emailService.SendAccountRegistration(
                driverDto.Email,
                subject,
                driverDto.FullName,
                driverDto.Email,
                driverDto.Password
            );

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
                        "Driver and files created successfully",
                        "Đã tạo tài xế và tệp đính kèm thành công",
                        null);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<DriverResponseDTO>(
                        true,
                        responseDto,
                        "Driver created successfully but there was an issue with file uploads",
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

            var driverProfileDetails = await _unitOfWork.DriverRepository.GetDriverProfileDetails(driverId);

            if (driverProfileDetails == null)
            {
                throw new InvalidOperationException("Failed to retrieve driver profile details");
            }

            return new ApiResponse<DriverProfileDetailsDTO>(
                true,
                driverProfileDetails,
                "Driver profile details retrieved successfully",
                null,
                null);
        }

        public async Task<ApiResponse<DriverResponseDTO>> UpdateDriverWithFiles(
    string driverId,
    UpdateDriverDTO updateDto,
    List<FileUploadDTO> newFiles,
    List<string> fileIdsToRemove,
    string userId)
        {
            var existingDriver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(driverId);
            if (existingDriver == null)
            {
                return new ApiResponse<DriverResponseDTO>(
                    false,
                    null,
                    "Driver not found",
                    "Không tìm thấy tài xế",
                    null);
            }

            //if (existingDriver.Email != updateDto.Email || existingDriver.PhoneNumber != updateDto.PhoneNumber)
            //{
            //    var contactValidation = await _unitOfWork.ContactHelper.ValidateContact(
            //        updateDto.Email,
            //        updateDto.PhoneNumber,
            //        driverId);

            //    if (!contactValidation.Success)
            //    {
            //        return new ApiResponse<DriverResponseDTO>(
            //            false,
            //            null,
            //            contactValidation.Message,
            //            contactValidation.MessageVN,
            //            null);
            //    }
            //}
            if (existingDriver.Email != updateDto.Email)
            {
                var existingEmail = _unitOfWork.DriverRepository.Get(d => d.Email == updateDto.Email);
                if (existingEmail != null)
                {
                    return new ApiResponse<DriverResponseDTO>(
                        false,
                        null,
                        "Email already exists",
                        "Email đã tồn tại",
                        null);
                }
            }

            if (existingDriver.PhoneNumber != updateDto.PhoneNumber)
            {
                var existingPhone = _unitOfWork.DriverRepository.Get(d => d.PhoneNumber == updateDto.PhoneNumber);
                if (existingPhone != null)
                {
                    return new ApiResponse<DriverResponseDTO>(
                        false,
                        null,
                        "Phone number already exists",
                        "Số điện thoại đã tồn tại",
                        null);
                }
            }

            existingDriver.FullName = updateDto.FullName;
            existingDriver.Email = updateDto.Email;
            existingDriver.PhoneNumber = updateDto.PhoneNumber;
            existingDriver.DateOfBirth = updateDto.DateOfBirth;
            existingDriver.ModifiedBy = userId;
            existingDriver.ModifiedDate = DateTime.Now;

            if (!string.IsNullOrEmpty(updateDto.Password))
            {
                existingDriver.Password = _passwordHasher.HashPassword(updateDto.Password);
            }

            var filesToAdd = new List<DriverFile>();
            if (newFiles != null && newFiles.Any())
            {
                foreach (var fileUpload in newFiles)
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
                        filesToAdd.Add(driverFile);
                    }
                }
            }

            var result = await _unitOfWork.DriverRepository.UpdateDriverWithFiles(
                existingDriver,
                filesToAdd,
                fileIdsToRemove);

            if (!result)
            {
                return new ApiResponse<DriverResponseDTO>(
                    false,
                    null,
                    "Failed to update driver",
                    "Cập nhật tài xế thất bại",
                    null);
            }

            var responseDto = new DriverResponseDTO
            {
                DriverId = existingDriver.DriverId,
                FullName = existingDriver.FullName,
                Email = existingDriver.Email,
                PhoneNumber = existingDriver.PhoneNumber,
                Status = existingDriver.Status,
                DateOfBirth = existingDriver.DateOfBirth,
                CreatedDate = existingDriver.CreatedDate
            };

            return new ApiResponse<DriverResponseDTO>(
                true,
                responseDto,
                "Driver updated successfully",
                "Cập nhật tài xế thành công",
                null);
        }

        public async Task<ApiResponse<bool>> UpdateDriverFileDetails(
            string fileId,
            FileDetailsDTO updateDto,
            string userId)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Invalid file ID",
                    "Mã tệp tin không hợp lệ",
                    "File ID cannot be empty");
            }

            var file = await _unitOfWork.DriverFileRepository.GetFileById(fileId);
            if (file == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "File not found",
                    "Không tìm thấy tệp tin",
                    $"No file found with ID: {fileId}");
            }

            var driver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(file.DriverId);
            if (driver == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Associated driver not found",
                    "Không tìm thấy tài xế liên kết",
                    "The file is not associated with a valid driver");
            }

            var result = await _unitOfWork.DriverRepository.UpdateDriverFileDetails(
                fileId,
                updateDto.Description,
                updateDto.Note,
                userId);

            if (!result)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Failed to update file details",
                    "Cập nhật thông tin tệp tin thất bại",
                    "Unable to update the file details");
            }

            return new ApiResponse<bool>(
                true,
                true,
                "File details updated successfully",
                "Cập nhật thông tin tệp tin thành công",
                null);
        }

        public async Task<ApiResponse<bool>> DeactivateDriver(string driverId, string userName)
        {
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(driverId);
            if (driver == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Driver not found",
                    "Không tìm thấy tài xế",
                    null);
            }
            if (driver.Status == (int)DriverStatus.OnDuty)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Cannot delete driver while on duty",
                    "Không thể xóa tài xế khi đang làm việc",
                    null);
            }
            driver.Status = (int)DriverStatus.Inactive;
            driver.ModifiedBy = userName;
            driver.ModifiedDate = DateTime.Now;
            await _unitOfWork.DriverRepository.UpdateAsync(driver);

            return new ApiResponse<bool>(
                true,
                true,
                "Driver deleted successfully",
                "Vô hiệu hóa tài xế thành công",
                null);
        }

        public async Task<ApiResponse<bool>> ActivateDriver(string driverId, string userName)
        {
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(driverId);
            if (driver == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Driver not found",
                    "Không tìm thấy tài xế",
                    null);
            }
            if (driver.Status != (int)DriverStatus.Inactive)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Cannot deactivate driver while on duty",
                    "Tài xế không hoạt động sẵn",
                    null);
            }
            driver.Status = (int)DriverStatus.Active;
            driver.DeletedBy = null;
            driver.DeletedDate = null;
            driver.ModifiedBy = userName;
            driver.ModifiedDate = DateTime.Now;
            await _unitOfWork.DriverRepository.UpdateAsync(driver);

            return new ApiResponse<bool>(
                true,
                true,
                "Driver deleted successfully",
                "Kích hoạt tài xế thành công",
                null);
        }

        public async Task<ApiResponse<bool>> DeleteDriver(string driverId, string userName)
        {
            var driver = await _unitOfWork.DriverRepository.GetByIdAsync(driverId);
            if (driver == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Driver not found",
                    "Không tìm thấy tài xế",
                    null);
            }
            if (driver.Status == (int)DriverStatus.OnDuty)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Cannot delete driver while on duty",
                    "Không thể xóa tài xế khi đang làm việc",
                    null);
            }
            driver.DeletedBy = userName;
            driver.DeletedDate = DateTime.Now;
            await _unitOfWork.DriverRepository.UpdateAsync(driver);

            return new ApiResponse<bool>(
                true,
                true,
                "Driver deleted successfully",
                "Xoá tài xế thành công",
                null);
        }

        public async Task<ApiResponse<DriverUseHistoryPagedDTO>> GetDriverUsageHistory(string driverId, PaginationParams paginationParams)
        {
            try
            {
                var driver = await _unitOfWork.DriverRepository.GetDriverByIdAsync(driverId);
                if (driver == null)
                {
                    return new ApiResponse<DriverUseHistoryPagedDTO>(
                        false,
                        null,
                        "Driver not found",
                        "Không tìm thấy tài xế",
                        $"No driver found with ID: {driverId}");
                }

                var useHistory = await _unitOfWork.DriverRepository.GetDriverUsageHistory(driverId, paginationParams);

                if (useHistory == null || useHistory.Items.Count == 0)
                {
                    var result = new DriverUseHistoryPagedDTO
                    {
                        DriverUseHistories = new PagedList<DriverUseHistory>(
                            new List<DriverUseHistory>(), 0, paginationParams.PageNumber, paginationParams.PageSize)
                    };

                    return new ApiResponse<DriverUseHistoryPagedDTO>(
                        true,
                        result,
                        "No use history found for this driver",
                        "Không tìm thấy lịch sử sử dụng cho tài xế này",
                        null);
                }

                var responseDto = new DriverUseHistoryPagedDTO
                {
                    DriverUseHistories = useHistory
                };

                return new ApiResponse<DriverUseHistoryPagedDTO>(
                    true,
                    responseDto,
                    $"Retrieved {useHistory.Items.Count} use history records (page {useHistory.CurrentPage} of {useHistory.TotalPages})",
                    $"Đã tìm thấy {useHistory.Items.Count} bản ghi lịch sử sử dụng phương tiện của tài xế {driver.FullName}",
                    null);
            }
            catch (Exception ex)
            {
                return new ApiResponse<DriverUseHistoryPagedDTO>(
                    false,
                    null,
                    "Failed to retrieve driver's usage history",
                    "Lấy lịch sử sử dụng phương tiện thất bại",
                    ex.Message);
            }
        }

    }
}
