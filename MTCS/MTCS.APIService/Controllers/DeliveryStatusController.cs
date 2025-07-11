﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/delivery-statuses")]
    [ApiController]

    public class DeliveryStatusController : ControllerBase
    {
        private readonly IDeliveryStatusService _deliveryStatusService;
        public DeliveryStatusController(IDeliveryStatusService deliveryStatusService)
        {
            _deliveryStatusService = deliveryStatusService;
        }
        [HttpGet]
        public async Task<IActionResult> GetDeliveryStatus()
        {
            var result = await _deliveryStatusService.GetDeliveryStatuses();
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateDeliveryStatus(List<CreateDeliveryStatusRequest> createDeliveries)
        {
            var currentUser = HttpContext.User;
            var result = await _deliveryStatusService.CreateDeliveryStatus(createDeliveries, currentUser);
            if(result.Status == 400)
            {
                return BadRequest(result);
            }
            else if (result.Status == 500)
            {
                return StatusCode(500, result);
            }
            return Ok(result);
        }
    }
}
