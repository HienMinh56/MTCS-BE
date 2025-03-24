using MTCS.Data.DTOs;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface ITractorService
    {
        Task<ApiResponse<TractorResponseDTO>> CreateTractor(CreateTractorDTO tractorDto, string userId);
    }
}
