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

        public TractorService(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<TractorResponseDTO>> CreateTractor(CreateTractorDTO tractorDto, string userId)
        {
            if (tractorDto.RegistrationExpirationDate <= tractorDto.RegistrationDate)
            {
                return new ApiResponse<TractorResponseDTO>(false, null, "Validation failed",
                    "Hạn đăng kiểm phải sau ngày đăng kiểm",
                    "Registration expiration date must be after registration date");
            }

            if (await _unitOfWork.TractorRepository.LicensePlateExist(tractorDto.LicensePlate))
            {
                return new ApiResponse<TractorResponseDTO>(false, null, "Validation failed", "Biển số đã tồn tại",
               "License plate already exists");
            }
            var tractorId = await _unitOfWork.TractorRepository.GenerateTractorId();

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
                Status = VehicleStatus.Active.ToString(),
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

            return new ApiResponse<TractorResponseDTO>(true, responseDto, "Create tractor successfully", "Tạo đầu kéo thành công", null);
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

            await _unitOfWork.TractorRepository.UpdateAsync(tractor);
            return new ApiResponse<bool>(
                true,
                true,
                "Tractor deleted successfully",
                "Xóa đầu kéo thành công",
                null);
        }

    }
}
