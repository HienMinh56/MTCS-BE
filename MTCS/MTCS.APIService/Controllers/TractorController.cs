using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
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
        public async Task<IActionResult> CreateTractor([FromBody] CreateTractorDTO tractorDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var response = await _tractorService.CreateTractor(tractorDto, userId);
            return Ok(response);
        }
    }
}
