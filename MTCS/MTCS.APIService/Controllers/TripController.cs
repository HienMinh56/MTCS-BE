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


        [HttpGet("get-trips")]
        public async Task<IActionResult> GetTripsFilters(string? driverId, string? tructorId, string? trailerId, string? status, string? orderId)
        {
            var result = await _tripService.GetTripsByFilterAsync(driverId, status, tructorId, trailerId, orderId);
            return Ok(result);
        }

        [HttpPost("update-trip-status")]
        public async Task<IActionResult> UpdateTripStatus(string tripId, string newStatusId)
        {
            var currentUser = HttpContext.User;
            var result = await _tripService.UpdateStatusTrip(tripId, newStatusId, currentUser);
            return Ok(result);
        }
    }
}
