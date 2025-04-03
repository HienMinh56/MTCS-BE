using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Service.Interfaces;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Staff")]
    public class TrailerController : ControllerBase
    {
        private readonly ITrailerService _trailerService;

        public TrailerController(ITrailerService trailerService)
        {
            _trailerService = trailerService;
        }

        [HttpPost("create-with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateTrailerWithFiles(
    [FromForm] CreateTrailerDTO trailerDto,
    [FromForm] List<TrailerFileUploadDTO> fileUploads)
        {
            var userId = User.GetUserId();

            var response = await _trailerService.CreateTrailerWithFiles(trailerDto, fileUploads, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetTrailersBasicInfo(
             [FromQuery] PaginationParams paginationParams,
             [FromQuery] string? searchKeyword = null,
             [FromQuery] TrailerStatus? status = null,
             [FromQuery] bool? maintenanceDueSoon = null,
             [FromQuery] bool? registrationExpiringSoon = null,
             [FromQuery] int? maintenanceDueDays = null,
             [FromQuery] int? registrationExpiringDays = null)
        {
            var response = await _trailerService.GetTrailersBasicInfo(
                paginationParams,
                searchKeyword,
                status,
                maintenanceDueSoon,
                registrationExpiringSoon,
                maintenanceDueDays,
                registrationExpiringDays);
            return Ok(response);
        }

        [HttpGet("{trailerId}")]
        public async Task<IActionResult> GetTrailerDetails(string trailerId)
        {
            var response = await _trailerService.GetTrailerDetail(trailerId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("activate-trailer/{trailerId}")]
        public async Task<IActionResult> ActivateTrailer(string trailerId)
        {
            var userId = User.GetUserId();

            var response = await _trailerService.ActivateTrailer(trailerId, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpPut("deactivate-trailer/{trailerId}")]
        public async Task<IActionResult> DeactivateTrailer(string trailerId)
        {
            var userId = User.GetUserId();

            var response = await _trailerService.DeleteTrailer(trailerId, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
