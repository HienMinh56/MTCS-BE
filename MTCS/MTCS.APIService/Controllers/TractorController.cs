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
    public class TractorController : ControllerBase
    {
        private readonly ITractorService _tractorService;

        public TractorController(ITractorService tractorService)
        {
            _tractorService = tractorService;
        }

        [HttpPost("create-tractor")]
        public async Task<IActionResult> CreateTractor([FromBody] CreateTractorDTO tractorDto)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.CreateTractor(tractorDto, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetTractorsBasicInfo(
             [FromQuery] PaginationParams paginationParams,
             [FromQuery] string? searchKeyword = null,
             [FromQuery] TractorStatus? status = null,
             [FromQuery] bool? maintenanceDueSoon = null,
             [FromQuery] bool? registrationExpiringSoon = null,
             [FromQuery] int? maintenanceDueDays = null,
             [FromQuery] int? registrationExpiringDays = null)
        {
            var response = await _tractorService.GetTractorsBasicInfo(
                paginationParams,
                searchKeyword,
                status,
                maintenanceDueSoon,
                registrationExpiringSoon,
                maintenanceDueDays,
                registrationExpiringDays);
            return Ok(response);
        }

        [HttpGet("{tractorId}")]
        public async Task<IActionResult> GetTractorDetails(string tractorId)
        {
            var response = await _tractorService.GetTractorDetail(tractorId);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPut("deactivate-tractor/{tractorId}")]
        public async Task<IActionResult> DeactivateTractor(string tractorId)
        {
            var userId = User.GetUserId();

            var response = await _tractorService.DeleteTractor(tractorId, userId);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
