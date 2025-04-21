using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriverDailyWorkingTimeController : ControllerBase
    {
        private readonly IDriverDailyWorkingTimeService _driverDailyWorkingTimeService;

        public DriverDailyWorkingTimeController(IDriverDailyWorkingTimeService driverDailyWorkingTimeService)
        {
            _driverDailyWorkingTimeService = driverDailyWorkingTimeService;
        }

        [HttpGet("total-time-day")]
        public async Task<IActionResult> GetTotalTimeByDriverAndDate([FromQuery] string driverId, [FromQuery] DateOnly workDate)
        {
            try
            {
                var result = await _driverDailyWorkingTimeService.GetTotalTimeByDriverAndDateAsync(driverId, workDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Status = -1,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("total-time-range")]
        public async Task<IActionResult> GetTotalTimeRange([FromQuery] string driverId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            var result = await _driverDailyWorkingTimeService.GetTotalTimeByRangeAsync(driverId, fromDate, toDate);
            return Ok(result);
        }
    }
}