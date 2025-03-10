using MTCS.Data.DTOs;
using MTCS.Data.Response;

namespace MTCS.Service.Interfaces
{
    public interface IDriverService
    {
        Task<ApiResponse<string>> CreateDriver(CreateDriverDTO driverDto);
    }
}
