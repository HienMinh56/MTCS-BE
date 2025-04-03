using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    public class TrailerService : ITrailerService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly IFirebaseStorageService _firebaseStorageService;

        public TrailerService(UnitOfWork unitOfWork, IFirebaseStorageService firebaseStorageService)
        {
            _unitOfWork = unitOfWork;
            _firebaseStorageService = firebaseStorageService;
        }

        public async Task<ApiResponse<TrailerResponseDTO>> CreateTrailerWithFiles(
    CreateTrailerDTO trailerDto,
    List<TrailerFileUploadDTO> fileUploads,
    string userId)
        {
            if (trailerDto.RegistrationExpirationDate <= trailerDto.RegistrationDate)
            {
                return new ApiResponse<TrailerResponseDTO>(false, null, "Validation failed",
                    "Hạn đăng kiểm phải sau ngày đăng kiểm",
                    "Registration expiration date must be after registration date");
            }

            var (exists, vehicleType, vehicleId, vehicleBrand) = await _unitOfWork.VehicleHelper.IsLicensePlateExist(trailerDto.LicensePlate);

            if (exists)
            {
                string vehicleTypeVN = _unitOfWork.VehicleHelper.GetVehicleTypeVN(vehicleType);

                return new ApiResponse<TrailerResponseDTO>(
                    false,
                    null,
                    "Validation failed",
                    $"Biển số đã được sử dụng bởi {vehicleTypeVN} {vehicleBrand} (ID: {vehicleId})",
                    $"License plate already exists on {vehicleType} {vehicleBrand} (ID: {vehicleId})");
            }

            var trailerId = await _unitOfWork.VehicleHelper.GenerateTrailerId();

            var createTrailer = new Trailer
            {
                TrailerId = trailerId,
                LicensePlate = trailerDto.LicensePlate,
                Brand = trailerDto.Brand,
                ManufactureYear = trailerDto.ManufactureYear,
                MaxLoadWeight = trailerDto.MaxLoadWeight,
                LastMaintenanceDate = trailerDto.LastMaintenanceDate,
                NextMaintenanceDate = trailerDto.NextMaintenanceDate,
                RegistrationDate = trailerDto.RegistrationDate,
                RegistrationExpirationDate = trailerDto.RegistrationExpirationDate,
                ContainerSize = trailerDto.ContainerSize,
                Status = TrailerStatus.Active.ToString(),
                CreatedDate = DateTime.Now,
                CreatedBy = userId,
                DeletedDate = null,
                DeletedBy = null
            };

            await _unitOfWork.TrailerRepository.CreateAsync(createTrailer);

            var responseDto = new TrailerResponseDTO
            {
                TrailerId = createTrailer.TrailerId,
                LicensePlate = createTrailer.LicensePlate,
                Brand = createTrailer.Brand,
                ManufactureYear = createTrailer.ManufactureYear,
                MaxLoadWeight = createTrailer.MaxLoadWeight,
                LastMaintenanceDate = createTrailer.LastMaintenanceDate,
                NextMaintenanceDate = createTrailer.NextMaintenanceDate,
                RegistrationDate = createTrailer.RegistrationDate,
                RegistrationExpirationDate = createTrailer.RegistrationExpirationDate,
                Status = createTrailer.Status,
                ContainerSize = (ContainerSize)createTrailer.ContainerSize.Value
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

                            var trailerFile = new TrailerFile
                            {
                                FileId = Guid.NewGuid().ToString(),
                                TrailerId = trailerId,
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

                            await _unitOfWork.TrailerFileRepository.CreateAsync(trailerFile);
                        }
                    }

                    return new ApiResponse<TrailerResponseDTO>(
                        true,
                        responseDto,
                        "Trailer and files created successfully",
                        "Đã tạo rơ moóc và tệp đính kèm thành công",
                        null);
                }
                catch (Exception ex)
                {
                    return new ApiResponse<TrailerResponseDTO>(
                        true,
                        responseDto,
                        "Trailer created successfully but there was an issue with file uploads",
                        "Đã tạo rơ moóc thành công nhưng có vấn đề khi upload file",
                        ex.Message);
                }
            }

            return new ApiResponse<TrailerResponseDTO>(
                true,
                responseDto,
                "Create trailer successfully",
                "Tạo rơ moóc thành công",
                null);
        }


        public async Task<ApiResponse<TrailerBasicInfoResultDTO>> GetTrailersBasicInfo(
           PaginationParams paginationParams,
           string? searchKeyword = null,
           TrailerStatus? status = null,
           bool? maintenanceDueSoon = null,
           bool? registrationExpiringSoon = null,
           int? maintenanceDueDays = null,
           int? registrationExpiringDays = null)
        {
            var infoResult = await _unitOfWork.TrailerRepository.GetTrailersBasicInfo(
                paginationParams,
                searchKeyword,
                status,
                maintenanceDueSoon,
                registrationExpiringSoon,
                maintenanceDueDays,
                registrationExpiringDays);

            if (infoResult.Trailers.TotalCount == 0)
            {
                return new ApiResponse<TrailerBasicInfoResultDTO>(
                    true,
                    infoResult,
                    "No trailers found",
                    "Không tìm thấy rơ moóc",
                    null);
            }

            return new ApiResponse<TrailerBasicInfoResultDTO>(
                true,
                infoResult,
                $"Retrieved {infoResult.Trailers.Items.Count} trailers of {infoResult.Trailers.TotalCount} total (page {infoResult.Trailers.CurrentPage} of {infoResult.Trailers.TotalPages})",
                null,
                null);
        }

        public async Task<ApiResponse<TrailerDetailsDTO>> GetTrailerDetail(string trailerId)
        {
            if (string.IsNullOrEmpty(trailerId))
            {
                return new ApiResponse<TrailerDetailsDTO>(
                    false,
                    null,
                    "Invalid trailer ID",
                    "Mã rơ moóc không hợp lệ",
                    "Trailer ID cannot be empty");
            }

            var trailer = await _unitOfWork.TrailerRepository.GetTrailerDetailsById(trailerId);

            if (trailer == null)
            {
                return new ApiResponse<TrailerDetailsDTO>(
                    false,
                    null,
                    "Trailer not found",
                    "Không tìm thấy rơ moóc",
                    $"No trailer found with ID: {trailerId}");
            }
            var trailerFiles = await _unitOfWork.TrailerFileRepository.GetFilesByTrailerId(trailerId);

            if (trailerFiles != null && trailerFiles.Any())
            {
                trailer.Files = trailerFiles.Select(file => new TrailerFileDTO
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

            return new ApiResponse<TrailerDetailsDTO>(
                true,
                trailer,
                "Trailer details retrieved successfully",
                null,
                null);
        }

        public async Task<ApiResponse<bool>> DeleteTrailer(string trailerId, string userId)
        {
            var trailer = await _unitOfWork.TrailerRepository.GetTrailerById(trailerId);
            if (trailer == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Trailer not found",
                    "Không tìm thấy rơ moóc",
                    $"No trailer found with ID: {trailerId}");
            }

            var (isInUse, activeTrips) = await _unitOfWork.TripRepository.IsTrailerInUseStatusNow(trailerId);

            if (isInUse)
            {
                var tripIds = string.Join(", ", activeTrips.Select(t => t.TripId));
                var tripInfo = activeTrips.Count == 1
                    ? $"Trip ID: {tripIds}"
                    : $"Trip IDs: {tripIds}";

                return new ApiResponse<bool>(
                    false,
                    false,
                    "Trailer is in use",
                    $"Rơ moóc đang trong hành trình: {tripIds}",
                    $"Cannot delete trailer that is in use in delivery trips: {tripIds}");
            }

            trailer.Status = TrailerStatus.Inactive.ToString();
            trailer.DeletedBy = userId;
            trailer.DeletedDate = DateTime.Now;

            await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
            return new ApiResponse<bool>(
                true,
                true,
                "Trailer deleted successfully",
                "Xóa rơ moóc thành công",
                null);
        }

        public async Task<ApiResponse<bool>> ActivateTrailer(string trailerId, string userId)
        {
            var trailer = await _unitOfWork.TrailerRepository.GetTrailerById(trailerId);
            if (trailer == null)
            {
                return new ApiResponse<bool>(
                    false,
                    false,
                    "Trailer not found",
                    "Không tìm thấy rơ moóc",
                    $"No trailer found with ID: {trailerId}");
            }

            if (trailer.Status != TrailerStatus.Inactive.ToString())
            {
                string currentStatus = trailer.Status;
                string statusVN = currentStatus == TrailerStatus.Active.ToString()
                    ? "đang hoạt động"
                    : "đang được sử dụng";

                return new ApiResponse<bool>(
                    false,
                    false,
                    $"Trailer is already {currentStatus.ToLower()}",
                    $"Đầu kéo đã {statusVN}",
                    $"Cannot activate a tractor that is already in {currentStatus.ToLower()} state");
            }

            trailer.Status = TrailerStatus.Active.ToString();
            trailer.DeletedBy = null;
            trailer.DeletedDate = null;
            trailer.ModifiedBy = userId;
            trailer.ModifiedDate = DateTime.Now;

            await _unitOfWork.TrailerRepository.UpdateAsync(trailer);
            return new ApiResponse<bool>(
                true,
                true,
                "Trailer activated successfully",
                "Kích hoạt rơ moóc thành công",
                null);
        }
    }
}
