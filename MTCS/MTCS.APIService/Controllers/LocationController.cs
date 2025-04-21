using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Handler;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LocationController : ControllerBase
    {
        public LocationController()
        {
            
        }
        [HttpGet("location/{userId}")]
        public IActionResult GetLocation(string userId)
        {
            var location = WebSocketHandler.GetLocationForUser(userId);
            if (location == null) return NotFound("No location");
            return Ok(location);
        }
    }
}
