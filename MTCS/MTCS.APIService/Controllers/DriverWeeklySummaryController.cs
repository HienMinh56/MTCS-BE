using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Services;
using static MTCS.Service.Services.DriverWeeklySummaryService;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriverWeeklySummaryController : ControllerBase
    {
        private readonly IDriverWeeklySummaryService _weeklySummaryService;

        public DriverWeeklySummaryController(IDriverWeeklySummaryService weeklySummaryService)
        {
            _weeklySummaryService = weeklySummaryService;
        }

        [HttpGet("weekly-time")]
        public async Task<IActionResult> GetWeeklyWorkingTime([FromQuery] string driverId)
        {
            var result = await _weeklySummaryService.GetWeeklyWorkingTimeAsync(driverId);
            return Ok(result);
        }
    }
}