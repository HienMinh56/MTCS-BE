﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/delivery-reports")]
    [ApiController]
    [Authorize]
    public class DeliveryReportController : ControllerBase
    {
        private readonly IDeliveryReportService _deliveryReportService;

        public DeliveryReportController(IDeliveryReportService deliveryReportService)
        {
            _deliveryReportService = deliveryReportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDeliveryReports(string? reportId, string? tripId, string? driverId)
        {
            var deliveryReports = await _deliveryReportService.GetDeliveryReport(reportId, tripId, driverId);
            return Ok(deliveryReports);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDeliveryReport([FromForm] CreateDeliveryReportRequest deliveryReport, [FromForm] List<IFormFile> files)
        {
            var currentUser = HttpContext.User;
            var result = await _deliveryReportService.CreateDeliveryReport(deliveryReport, files, currentUser);
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateDeliveryReport([FromForm] UpdateDeliveryReportRequest deliveryReport)
        {
            var currentUser = HttpContext.User;
            var result = await _deliveryReportService.UpdateDeliveryReport(deliveryReport, currentUser);
            return Ok(result);
        }
    }
}
