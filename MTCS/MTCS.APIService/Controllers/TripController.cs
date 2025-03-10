using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Interfaces;
using System.Security.Claims;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpGet("assigned-trips")]
        public async Task<IActionResult> GetDriverAssignedTrips()
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(driverId))
            {
                return Unauthorized();
            }
            var response = await _tripService.GetDriverAssignedTrips(driverId);
            return Ok(response);
        }

        [HttpGet("{tripId}")]
        public async Task<IActionResult> GetTripDetails(string tripId)
        {
            var driverId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(driverId))
            {
                return Unauthorized();
            }

            var response = await _tripService.GetTripDetails(tripId);
            return Ok(response);
        }
    }
}
