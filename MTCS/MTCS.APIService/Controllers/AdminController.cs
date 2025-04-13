using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.DTOs;
using MTCS.Data.Enums;
using MTCS.Data.Response;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("revenue")]
        public async Task<ActionResult<ApiResponse<RevenueAnalyticsDTO>>> GetRevenueAnalytics(
           [FromQuery] RevenuePeriodType periodType,
           [FromQuery] DateTime startDate,
           [FromQuery] DateTime? endDate = null)
        {
            var response = await _adminService.GetRevenueAnalyticsAsync(periodType, startDate, endDate);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("revenue/customers")]
        public async Task<ActionResult<ApiResponse<List<CustomerRevenueDTO>>>> GetRevenueByCustomer(
            [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var response = await _adminService.GetRevenueByCustomerAsync(startDate, endDate);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("trips/financial/{tripId}")]
        public async Task<ActionResult<ApiResponse<TripFinancialDTO>>> GetTripFinancialDetails(string tripId)
        {
            var response = await _adminService.GetTripFinancialDetailsAsync(tripId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("trips/financial")]
        public async Task<ActionResult<ApiResponse<List<TripFinancialDTO>>>> GetTripsFinancialDetails(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string customerId = null)
        {
            var response = await _adminService.GetTripsFinancialDetailsAsync(startDate, endDate, customerId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("profit")]
        public async Task<ActionResult<ApiResponse<ProfitAnalyticsDTO>>> GetProfitAnalytics(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var response = await _adminService.GetProfitAnalyticsAsync(startDate, endDate);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("fuel/average-cost")]
        public async Task<ActionResult<ApiResponse<decimal>>> GetAverageFuelCostPerDistance(
            [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var response = await _adminService.GetAverageFuelCostPerDistanceAsync(startDate, endDate);
            return response.Success ? Ok(response) : BadRequest(response);
        }
    }
}
