using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class TractorService : ITractorService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;

        public TractorService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
        }

        public async Task<ApiResponse<TractorResponseDTO>> CreateTractorWithFiles(
    CreateTractorDTO tractorDto,
    List<TractorFileUploadDTO> fileUploads,
    string userId)
        {
            if (tractorDto.RegistrationExpirationDate <= tractorDto.RegistrationDate)
            {
                return new ApiResponse<TractorResponseDTO>(false, null, "Validation failed",
                    "Hạn đăng kiểm phải sau ngày đăng kiểm",
                    "Registration expiration date must be after registration date");
            }

            var (exists, vehicleType, vehicleId, vehicleBrand) = await _unitOfWork.VehicleHelper.IsLicensePlateExist(tractorDto.LicensePlate);

            if (exists)
            {
                string vehicleTypeVN = _unitOfWork.VehicleHelper.GetVehicleTypeVN(vehicleType);

                return new ApiResponse<TractorResponseDTO>(
                    false,
                    null,
                    "Validation failed",
                    $"Biển số đã được sử dụng bởi {vehicleTypeVN} {vehicleBrand} (ID: {vehicleId})",
                    $"License plate already exists on {vehicleType} {vehicleBrand} (ID: {vehicleId})");
            }
            var tractorId = await _unitOfWork.VehicleHelper.GenerateTractorId();

            var createTractor = new Tractor
            {
                TractorId = tractorId,
                LicensePlate = tractorDto.LicensePlate,
                Brand = tractorDto.Brand,
                ManufactureYear = tractorDto.ManufactureYear,
                MaxLoadWeight = tractorDto.MaxLoadWeight,
                LastMaintenanceDate = tractorDto.LastMaintenanceDate,
                NextMaintenanceDate = tractorDto.NextMaintenanceDate,
                RegistrationDate = tractorDto.RegistrationDate,
                RegistrationExpirationDate = tractorDto.RegistrationExpirationDate,
                ContainerType = tractorDto.ContainerType,
                Status = TractorStatus.Active.ToString(),
                CreatedDate = DateTime.Now,
                CreatedBy = userId,
                DeletedDate = null,
                DeletedBy = null
            };

            await _unitOfWork.TractorRepository.CreateAsync(createTractor);

            var responseDto = new TractorResponseDTO
            {
                TractorId = createTractor.TractorId,
                LicensePlate = createTractor.LicensePlate,
                Brand = createTractor.Brand,
                ManufactureYear = createTractor.ManufactureYear,
                MaxLoadWeight = createTractor.MaxLoadWeight,
                LastMaintenanceDate = createTractor.LastMaintenanceDate,
                NextMaintenanceDate = createTractor.NextMaintenanceDate,
                RegistrationDate = createTractor.RegistrationDate,
                RegistrationExpirationDate = createTractor.RegistrationExpirationDate,
                Status = createTractor.Status,
                ContainerType = (ContainerType)createTractor.ContainerType.Value
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

                            var tractorFile = new TractorFile
                            {
                                FileId = Guid.NewGuid().ToString(),
                                TractorsId = tractorId,
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

                            await _unitOfWork.TractorFileRepository.CreateAsync(tractorFile);
                        }
                    }

                    return new ApiResponse<TractorResponseDTO>(
                        true,
                        responseDto,
                        "Tractor and files created successfully",
                        "Đã tạo đầu kéo và tệp đính kèm thành công",
                        null);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<TractorResponseDTO>(
                        true,
                        responseDto,
                        "Tractor created successfully but there was an issue with file uploads",
                        "Đã tạo đầu kéo thành công nhưng có vấn đề khi upload file",
                        ex.Message);
                }
            }

            return new ApiResponse<TractorResponseDTO>(
                true,
                responseDto,
                "Create tractor successfully",
                "Tạo đầu kéo thành công",
                null);
        }

        public async Task<ApiResponse<TractorBasicInfoResultDTO>> GetTractorsBasicInfo(
           PaginationParams paginationParams,
           string? searchKeyword = null,
           TractorStatus? status = null,
           bool? maintenanceDueSoon = null,
           bool? registrationExpiringSoon = null,
           int? maintenanceDueDays = null,
           int? registrationExpiringDays = null)
        {
            var infoResult = await _unitOfWork.TractorRepository.GetTractorsBasicInfo(
                paginationParams,
                searchKeyword,
                status,
                maintenanceDueSoon,
                registrationExpiringSoon,
                maintenanceDueDays,
                registrationExpiringDays);

            if (infoResult.Tractors.TotalCount == 0)
            {
                return new ApiResponse<TractorBasicInfoResultDTO>(
                    true,
                    infoResult,
                    "No tractors found",
                    "Không tìm thấy đầu kéo",
                    null);
            }

            return new ApiResponse<TractorBasicInfoResultDTO>(
                true,
                infoResult,
                $"Retrieved {infoResult.Tractors.Items.Count} tractors of {infoResult.Tractors.TotalCount} total (page {infoResult.Tractors.CurrentPage} of {infoResult.Tractors.TotalPages})",
                null,
                null);
        }

        public async Task<ApiResponse<TractorDetailsDTO>> GetTractorDetail(string tractorId)
        {
            if (string.IsNullOrEmpty(tractorId))
            {
                return new ApiResponse<TractorDetailsDTO>(
                    false,
                    null,
                    "Invalid tractor ID",
                    "Mã đầu kéo không hợp lệ",
                    "Tractor ID cannot be empty");
            }

            var tractor = await _unitOfWork.TractorRepository.GetTractorDetailsById(tractorId);

            if (tractor == null)
            {
                return new ApiResponse<TractorDetailsDTO>(
                    false,
                    null,
                    "Tractor not found",
                    "Không tìm thấy đầu kéo",
                    $"No tractor found with ID: {tractorId}");
            }

            var tractorFiles = await _unitOfWork.TractorFileRepository.GetFilesByTractorId(tractorId);

            if (tractorFiles != null && tractorFiles.Any())
            {
                tractor.Files = tractorFiles.Select(file => new TractorFileDTO
                {
                    FileId = file.FileId,
                    FileName = file.FileName,
                    FileUrl = file.FileUrl,
                    FileType = file.FileType,
                    Description = file.Description,
                    Note = file.Note,
                    UploadDate = file.UploadDate ?? DateTime.MinValue,
                    UploadBy = file.UploadBy
                }).ToList();
            }

            return new ApiResponse<TractorDetailsDTO>(
                true,
                tractor,
                "Tractor details retrieved successfully",
                null,
                null);
        }

        public async Task<ApiResponse<bool>> DeleteTractor(string tractorId, string userId)
        {
            var tractor = await _unitOfWork.TractorRepository.GetTractorById(tractorId);
            if (tractor == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Tractor not found",
                    "Không tìm thấy đầu kéo",
                    $"No tractor found with ID: {tractorId}");
            }

            var (isInUse, activeTrips) = await _unitOfWork.TripRepository.IsTractorInUseStatusNow(tractorId);

            if (isInUse)
            {
                var tripIds = string.Join(", ", activeTrips.Select(t => t.TripId));
                var tripInfo = activeTrips.Count == 1
                    ? $"Trip ID: {tripIds}"
                    : $"Trip IDs: {tripIds}";

                return new ApiResponse<bool>(
                    false,
                    false,
                    "Tractor is in use",
                    $"Đầu kéo đang trong hành trình: {tripIds}",
                    $"Cannot delete tractor that is in use in delivery trips: {tripIds}");
            }

            tractor.Status = VehicleStatus.Inactive.ToString();
            tractor.DeletedBy = userId;
            tractor.DeletedDate = DateTime.Now;
            tractor.ModifiedBy = userId;
            tractor.ModifiedDate = DateTime.Now;

            await _unitOfWork.TractorRepository.UpdateAsync(tractor);
            return new ApiResponse<bool>(
                true,
                true,
                "Tractor deleted successfully",
                "Vô hiệu hoá đầu kéo thành công",
                null);
        }

        public async Task<ApiResponse<bool>> ActivateTractor(string tractorId, string userId)
        {
            var tractor = await _unitOfWork.TractorRepository.GetTractorById(tractorId);
            if (tractor == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Tractor not found",
                    "Không tìm thấy đầu kéo",
                    $"No tractor found with ID: {tractorId}");
            }

            if (tractor.Status != TractorStatus.Inactive.ToString())
            {
                string currentStatus = tractor.Status;
                string statusVN = currentStatus == TractorStatus.Active.ToString()
                    ? "đang hoạt động"
                    : "đang được sử dụng";

                return new ApiResponse<bool>(
                    false,
                    false,
                    $"Tractor is already {currentStatus.ToLower()}",
                    $"Đầu kéo đã {statusVN}",
                    $"Cannot activate a tractor that is already in {currentStatus.ToLower()} state");
            }

            tractor.Status = VehicleStatus.Active.ToString();
            tractor.ModifiedBy = userId;
            tractor.ModifiedDate = DateTime.Now;
            tractor.DeletedBy = null;
            tractor.DeletedDate = null;

            await _unitOfWork.TractorRepository.UpdateAsync(tractor);
            return new ApiResponse<bool>(
                true,
                true,
                "Tractor deleted successfully",
                "Kích hoạt đầu kéo thành công",
                null);
        }

    }
}
