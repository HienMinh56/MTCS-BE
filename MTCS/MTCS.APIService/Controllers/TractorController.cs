using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Helpers;
using MTCS.Service.Interfaces;
using System.Security.Claims;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TractorController : ControllerBase
    {
        private readonly ITractorService _tractorService;

        public TractorController(ITractorService tractorService)
        {
            _tractorService = tractorService;
        }

        [HttpPost("create-tractor")]
        [Authorize]
        public async Task<IActionResult> CreateTractor([FromBody] CreateTractorDTO tractorDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

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
             [FromQuery] TractorStatus? status = null,
             [FromQuery] bool? maintenanceDueSoon = null,
             [FromQuery] bool? registrationExpiringSoon = null,
             [FromQuery] int? maintenanceDueDays = null,
             [FromQuery] int? registrationExpiringDays = null)
        {
            var response = await _tractorService.GetTractorsBasicInfo(
                paginationParams,
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
    }
}
