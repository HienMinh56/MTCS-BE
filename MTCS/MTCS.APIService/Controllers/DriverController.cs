using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Helpers;
using MTCS.Service.Interfaces;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;

        public DriverController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers([FromQuery] PaginationParams paginationParams, int? status = null)
        {
            var response = await _driverService.ViewDrivers(paginationParams, status);
            return Ok(response);
        }

        [HttpGet("{driverId}/profile")]
        public async Task<IActionResult> GetDriverProfile(string driverId)
        {
            var response = await _driverService.GetDriverProfile(driverId);
            return Ok(response);
        }
    }
}
