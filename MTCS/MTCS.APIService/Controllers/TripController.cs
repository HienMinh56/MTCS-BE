using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.Helpers;
using MTCS.Data.Request;
using MTCS.Service.Base;
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
            var result = await _tripService.GetTripsByFilterAsync(tripId, driverId, status, tractorId, trailerId, orderId);
            return Ok(result);
        }


        [HttpPatch("{tripId}/status")]
        public async Task<IActionResult> UpdateTripStatus(
            [FromRoute] string tripId,
            [FromBody] string newStatusId)
        {
            var userId = User.GetUserId();
            var result = await _tripService.UpdateStatusTrip(tripId, newStatusId, userId);
            return Ok(result);
        }

        [HttpPut("update/{tripId}")]
        public async Task<IActionResult> UpdateTrip(string tripId, [FromQuery] UpdateTripRequest model)
        {
           
            var result = await _tripService.UpdateTripAsync(tripId, model, User);
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
