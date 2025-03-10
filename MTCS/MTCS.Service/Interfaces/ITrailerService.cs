using MTCS.Data.DTOs;
using MTCS.Data.Models;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITrailerService
    {
        Task<ApiResponse<TrailerResponseDTO>> CreateTrailer(CreateTrailerDTO trailerDto, string userId);
        Task<ApiResponse<TrailerCategory>> CreateTrailerCategory(CategoryCreateDTO categoryDto);
    }
}
