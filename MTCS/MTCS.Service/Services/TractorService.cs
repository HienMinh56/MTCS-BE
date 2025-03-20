using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    //public class TractorService : ITractorService
    //{
    //    private readonly UnitOfWork _unitOfWork;

    //    public TractorService(UnitOfWork unitOfWork)
    //    {
    //        _unitOfWork = unitOfWork;
    //    }

    //    public async Task<ApiResponse<TractorCategory>> CreateTractorCategory(CategoryCreateDTO categoryDto)
    //    {
    //        var existingCategories = await _unitOfWork.TractorRepository.GetAllTractorCategories();

    //        if (existingCategories.Any(c => c.CategoryName.Equals(categoryDto.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
    //        {
    //            return new ApiResponse<TractorCategory>(false, null, "Category already exists", "A category with this name already exists");
    //        }

    //        string categoryId = await CategoryIDGenerator.GenerateTractorCategoryId(_unitOfWork);

    //        var createTractorCategory = new TractorCategory
    //        {
    //            TractorCateId = categoryId,
    //            CategoryName = categoryDto.CategoryName.Trim()
    //        };

    //        await _unitOfWork.TractorRepository.CreateCategory(createTractorCategory);
    //        return new ApiResponse<TractorCategory>(true, createTractorCategory, "Create tractor category sucessfully", null);
    //    }

    //    public async Task<ApiResponse<TractorResponseDTO>> CreateTractor(CreateTractorDTO tractorDto, string userId)
    //    {
    //        if (tractorDto.RegistrationExpirationDate <= tractorDto.RegistrationDate)
    //        {
    //            return new ApiResponse<TractorResponseDTO>(false, null, "Validation failed",
    //                "Registration expiration date must be after registration date");
    //        }

    //        var category = await _unitOfWork.TractorRepository.GetCategoryById(tractorDto.TractorCateId);
    //        if (category == null)
    //        {
    //            return new ApiResponse<TractorResponseDTO>(false, null, "Invalid category",
    //                "Tractor category does not exist");
    //        }

    //        var createTractor = new Tractor
    //        {
    //            TractorId = Guid.NewGuid().ToString(),
    //            LicensePlate = tractorDto.LicensePlate,
    //            Brand = tractorDto.Brand,
    //            Model = tractorDto.Model,
    //            ManufactureYear = tractorDto.ManufactureYear,
    //            MaxLoadWeight = tractorDto.MaxLoadWeight,
    //            LastMaintenanceDate = tractorDto.LastMaintenanceDate,
    //            NextMaintenanceDate = tractorDto.NextMaintenanceDate,
    //            RegistrationDate = tractorDto.RegistrationDate,
    //            RegistrationExpirationDate = tractorDto.RegistrationExpirationDate,
    //            TractorCateId = tractorDto.TractorCateId,
    //            Status = VehicleStatus.Active.ToString(),
    //            CreatedDate = DateTime.Now,
    //            CreatedBy = userId,
    //            DeletedDate = null,
    //            DeletedBy = null
    //        };

    //        await _unitOfWork.TractorRepository.CreateAsync(createTractor);

    //        var responseDto = new TractorResponseDTO
    //        {
    //            TractorId = createTractor.TractorId,
    //            LicensePlate = createTractor.LicensePlate,
    //            Brand = createTractor.Brand,
    //            Model = createTractor.Model,
    //            ManufactureYear = createTractor.ManufactureYear,
    //            MaxLoadWeight = createTractor.MaxLoadWeight,
    //            LastMaintenanceDate = createTractor.LastMaintenanceDate,
    //            NextMaintenanceDate = createTractor.NextMaintenanceDate,
    //            RegistrationDate = createTractor.RegistrationDate,
    //            RegistrationExpirationDate = createTractor.RegistrationExpirationDate,
    //            Status = createTractor.Status,
    //            Category = new TractorCategoryResponseDTO
    //            {
    //                TractorCateId = category.TractorCateId,
    //                CategoryName = category.CategoryName
    //            }
    //        };

    //        return new ApiResponse<TractorResponseDTO>(true, responseDto, "Create tractor successfully", null);

    //    }
    //}
}
