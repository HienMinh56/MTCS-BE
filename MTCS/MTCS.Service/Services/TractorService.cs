using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
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
                    "Registration expiration date must be after registration date");
            }

            if (await _unitOfWork.TractorRepository.LicensePlateExist(tractorDto.LicensePlate))
            {
                return new ApiResponse<TractorResponseDTO>(false, null, "Validation failed",
               "License plate already exists");
            }

            var createTractor = new Tractor
            {
                TractorId = Guid.NewGuid().ToString(),
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

            return new ApiResponse<TractorResponseDTO>(true, responseDto, "Create tractor successfully", null);
        }
    }
}
