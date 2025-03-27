using Microsoft.AspNetCore.Mvc;
using MTCS.Service.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MTCS.APIService.Controllers
{
    [Route("api/trips")]
    [ApiController]
    public class TripController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripController(ITripService tripService)
        {
            _tripService = tripService;
        }


        [HttpGet]
        public async Task<IActionResult> GetTrips(
            [FromQuery] string? tripId,
            [FromQuery] string? driverId,
            [FromQuery] string? tractorId,
            [FromQuery] string? trailerId,
            [FromQuery] string? status,
            [FromQuery] string? orderId)
        {
            var result = await _tripService.GetTripsByFilterAsync(tripId,driverId, status, tractorId, trailerId, orderId);
            return Ok(result);
        }


        [HttpPatch("{tripId}/status")]
        public async Task<IActionResult> UpdateTripStatus(
            [FromRoute] string tripId,
            [FromBody] string newStatusId)
        {
            var currentUser = HttpContext.User;
            var result = await _tripService.UpdateStatusTrip(tripId, newStatusId, currentUser);
            return Ok(result);
        }
    }
}
