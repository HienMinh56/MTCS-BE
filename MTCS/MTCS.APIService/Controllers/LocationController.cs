using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Handler;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly WebSocketHandler _webSocketHandler;
        public LocationController(WebSocketHandler webSocketHandler)
        {
            _webSocketHandler = webSocketHandler;
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
