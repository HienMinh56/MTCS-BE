using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Helpers;
using MTCS.Service.Interfaces;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;

        public DriverController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers([FromQuery] PaginationParams paginationParams, int? status = null, string? keyword = null)
        {
            var response = await _driverService.ViewDrivers(paginationParams, status, keyword);
            return Ok(response);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetDriverProfile(string driverId)
        {
            var response = await _driverService.GetDriverProfile(driverId);
            return Ok(response);
        }

        [HttpPut("{driverId}")]
        public async Task<IActionResult> UpdateDriverWithFiles(string driverId, [FromForm] UpdateDriverDTO updateDto,
            [FromForm] List<FileUploadDTO>? newFiles = null,
            [FromForm] List<string>? fileIdsToRemove = null)
        {
            var userId = User.GetUserId();

            var response = await _driverService.UpdateDriverWithFiles(driverId, updateDto, newFiles ?? new List<FileUploadDTO>(),
                fileIdsToRemove ?? new List<string>(), userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("file/{fileId}")]
        public async Task<IActionResult> UpdateDriverFileDetails(string fileId, [FromBody] FileDetailsDTO updateDto)
        {
            var userId = User.GetUserId();
            var response = await _driverService.UpdateDriverFileDetails(fileId, updateDto, userId);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("activate-driver/{driverId}")]
        public async Task<IActionResult> ActivateDriver(string driverId)
        {
            var userName = User.GetUserName();
            var response = await _driverService.ActivateDriver(driverId, userName);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("deactivate-driver/{driverId}")]
        public async Task<IActionResult> DeactivateDriver(string driverId)
        {
            var userName = User.GetUserName();
            var response = await _driverService.DeactivateDriver(driverId, userName);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut("delete-driver/{driverId}")]
        public async Task<IActionResult> DeleteDriver(string driverId)
        {
            var userName = User.GetUserName();
            var response = await _driverService.DeleteDriver(driverId, userName);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("history/{driverId}")]
        public async Task<IActionResult> GetDriverUsageHistory(string driverId, [FromQuery] PaginationParams paginationParams)
        {
            var response = await _driverService.GetDriverUsageHistory(driverId, paginationParams);
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("{driverId}/time-table")]
        public async Task<IActionResult> GetDriverTimeTable(string driverId, DateTime startOfWeek, DateTime endOfWeek)
        {
            var result = await _driverService.GetDriverTimeTable(driverId, startOfWeek, endOfWeek);
            return Ok(result);
        }

        [HttpGet("time-table")]
        public async Task<IActionResult> GetAllDriversTimeTable(DateTime startOfWeek, DateTime endOfWeek)
        {
            var result = await _driverService.GetAllDriversTimeTable(startOfWeek, endOfWeek);
            return Ok(result);
        }
    }
}
