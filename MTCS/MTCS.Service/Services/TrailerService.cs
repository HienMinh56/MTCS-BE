using MTCS.Data;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Data.Models;
using MTCS.Data.Response;
using MTCS.Service.Interfaces;

namespace MTCS.Service.Services
{
    //public class TrailerService : ITrailerService
    //{
    //    private readonly UnitOfWork _unitOfWork;

    //    public TrailerService(UnitOfWork unitOfWork)
    //    {
    //        _unitOfWork = unitOfWork;
    //    }

    //    public async Task<ApiResponse<TrailerCategory>> CreateTrailerCategory(CategoryCreateDTO categoryDto)
    //    {
    //        var existingCategories = await _unitOfWork.TrailerRepository.GetAllTrailerCategories();

    //        if (existingCategories.Any(c => c.CategoryName.Equals(categoryDto.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase)))
    //        {
    //            return new ApiResponse<TrailerCategory>(false, null, "Category already exists", "A category with this name already exists");
    //        }

    //        string categoryId = await CategoryIDGenerator.GenerateTrailerCategoryId(_unitOfWork);

    //        var createTrailerCategory = new TrailerCategory
    //        {
    //            TrailerCateId = categoryId,
    //            CategoryName = categoryDto.CategoryName.Trim()
    //        };

    //        await _unitOfWork.TrailerRepository.CreateTrailerCategory(createTrailerCategory);
    //        return new ApiResponse<TrailerCategory>(true, createTrailerCategory, "Create trailer category successfully", null);
    //    }

    //    public async Task<ApiResponse<TrailerResponseDTO>> CreateTrailer(CreateTrailerDTO trailerDto, string userId)
    //    {
    //        if (trailerDto.RegistrationExpirationDate <= trailerDto.RegistrationDate)
    //        {
    //            return new ApiResponse<TrailerResponseDTO>(false, null, "Validation failed",
    //                "Registration expiration date must be after registration date");
    //        }

    //        var category = await _unitOfWork.TrailerRepository.GetCategoryById(trailerDto.TrailerCateId);
    //        if (category == null)
    //        {
    //            return new ApiResponse<TrailerResponseDTO>(false, null, "Invalid category",
    //                "Trailer category does not exist");
    //        }

    //        var createTrailer = new Trailer
    //        {
    //            TrailerId = Guid.NewGuid().ToString(),
    //            LicensePlate = trailerDto.LicensePlate,
    //            Brand = trailerDto.Brand,
    //            Model = trailerDto.Model,
    //            Length = trailerDto.Length,
    //            ManufactureYear = trailerDto.ManufactureYear,
    //            MaxLoadWeight = trailerDto.MaxLoadWeight,
    //            LastMaintenanceDate = trailerDto.LastMaintenanceDate,
    //            NextMaintenanceDate = trailerDto.NextMaintenanceDate,
    //            RegistrationDate = trailerDto.RegistrationDate,
    //            RegistrationExpirationDate = trailerDto.RegistrationExpirationDate,
    //            TrailerCateId = trailerDto.TrailerCateId,
    //            Status = VehicleStatus.Active.ToString(),
    //            CreatedDate = DateTime.Now,
    //            CreatedBy = userId,
    //            DeletedDate = null,
    //            DeletedBy = null
    //        };

    //        await _unitOfWork.TrailerRepository.CreateAsync(createTrailer);

    //        var responseDto = new TrailerResponseDTO
    //        {
    //            TrailerId = createTrailer.TrailerId,
    //            LicensePlate = createTrailer.LicensePlate,
    //            Brand = createTrailer.Brand,
    //            Model = createTrailer.Model,
    //            Length = createTrailer.Length,
    //            ManufactureYear = createTrailer.ManufactureYear,
    //            MaxLoadWeight = createTrailer.MaxLoadWeight,
    //            LastMaintenanceDate = createTrailer.LastMaintenanceDate,
    //            NextMaintenanceDate = createTrailer.NextMaintenanceDate,
    //            RegistrationDate = createTrailer.RegistrationDate,
    //            RegistrationExpirationDate = createTrailer.RegistrationExpirationDate,
    //            Status = createTrailer.Status,
    //            Category = new TrailerCategoryResponseDTO
    //            {
    //                TrailerCateId = category.TrailerCateId,
    //                CategoryName = category.CategoryName
    //            }
    //        };

    //        return new ApiResponse<TrailerResponseDTO>(true, responseDto, "Create trailer successfully", null);
    //    }
    //}
}
