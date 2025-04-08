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
    }
}
