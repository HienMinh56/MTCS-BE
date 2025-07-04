﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.Helpers;
using MTCS.Data.Request;
using MTCS.Service.Interfaces;

namespace MTCS.APIService.Controllers
{
    [Route("api/trips")]
    [ApiController]
    [Authorize]
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
            [FromQuery] string? orderId,
            [FromQuery] string? trackingCode,
            [FromQuery] string? tractorlicensePlate,
            [FromQuery] string? trailerlicensePlate
            )
        {
            var result = await _tripService.GetTripsByFilterAsync(tripId, driverId, status, tractorId, trailerId, orderId, trackingCode, tractorlicensePlate, trailerlicensePlate);
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
        public async Task<IActionResult> UpdateTrip(string tripId, [FromForm] UpdateTripRequest model)
        {

            var result = await _tripService.UpdateTripAsync(tripId, model, User);
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPut("cancel")]
        public async Task<IActionResult> Cancel([FromBody] CancelTripRequest request)
        {

            var result = await _tripService.CancelTrip(request, User);
            if (result.Status == Const.SUCCESS_UPDATE_CODE)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTrip([FromForm] CreateTripRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var trip = await _tripService.CreateTripAsync(model, User);
                return Ok(trip);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("auto-schedule")]
        public async Task<IActionResult> AutoScheduleTripsForOrder([FromForm] string orderDetailId)
        {
            if (string.IsNullOrEmpty(orderDetailId))
                return BadRequest("OrderId không hợp lệ!");
            try
            {
                var result = await _tripService.AutoScheduleTripsForOrderAsync(orderDetailId);
                if (result.Status < 0)
                    return BadRequest(result.Message);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi: {ex.Message}");
            }
        }

        [HttpGet("getAll")]
        public async Task<IActionResult> GetAllTrips()
        {
            var result = await _tripService.GetAllTripsAsync();
            return Ok(result);
        }
        [HttpGet("getTripsMo")]
        public async Task<IActionResult> GetTripsByGroupAsync(string driverId, string groupType)
        {
            var result = await _tripService.GetTripsByGroupAsync(driverId, groupType);
            return Ok(result);
        }

        [HttpGet("time-table")]
        public async Task<IActionResult> GetTimeTable(DateTime startOfWeek, DateTime endOfWeek)
        {
            var result = await _tripService.GetTripTimeTable(startOfWeek, endOfWeek);
            return Ok(result);
        }
    }
}
