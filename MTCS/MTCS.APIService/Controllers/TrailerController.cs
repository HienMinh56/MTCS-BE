using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Service.Interfaces;
using System.Security.Claims;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrailerController : ControllerBase
    {
        private readonly ITrailerService _trailerService;

        public TrailerController(ITrailerService trailerService)
        {
            _trailerService = trailerService;
        }

        [HttpPost("create-trailer-category")]
        public async Task<IActionResult> CreateTrailerCategory([FromBody] CategoryCreateDTO categoryDto)
        {
            var response = await _trailerService.CreateTrailerCategory(categoryDto);
            return Ok(response);
        }

        [HttpPost("create-trailer")]
        public async Task<IActionResult> CreateTrailer([FromBody] CreateTrailerDTO trailerDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var response = await _trailerService.CreateTrailer(trailerDto, userId);
            return Ok(response);
        }
    }
}
