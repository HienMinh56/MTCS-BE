using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/fuel-reports")]
    [ApiController]
    public class FuelReportController : ControllerBase
    {
        private readonly IFuelReportService _fuelReportService;

        public FuelReportController(IFuelReportService fuelReportService)
        {
            _fuelReportService = fuelReportService;
        }

        [HttpGet("get-fuel-reports")]
        public async Task<IActionResult> GetFuelReports(string? reportId, string? tripId, string? driverId)
        {
            var fuelReports = await _fuelReportService.GetFuelReport(reportId, tripId, driverId);
            return Ok(fuelReports);
        }

        [HttpPost("create-fuel-report")]
        public async Task<IActionResult> CreateFuelReport([FromForm] CreateFuelReportRequest fuelReport, [FromForm] List<IFormFile> files)
        {
            var currentUser = HttpContext.User;
            var result = await _fuelReportService.CreateFuelReport(fuelReport, files, currentUser);
            return Ok(result);
        }

        [HttpPut("update-fuel-report")]
        public async Task<IActionResult> UpdateFuelReport([FromForm] UpdateFuelReportRequest fuelReport)
        {
            var currentUser = HttpContext.User;
            var result = await _fuelReportService.UpdateFuelReport(fuelReport, currentUser);
            return Ok(result);
        }
    }
}
