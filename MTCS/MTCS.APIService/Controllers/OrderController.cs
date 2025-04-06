using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.Request;
using MTCS.Service.Services;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace MTCS.APIService.Controllers
{
    [Route("api/order")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? orderId,
            [FromQuery] string? tripId,
            [FromQuery] string? userId,
            [FromQuery] int? containerType,
            [FromQuery] string? containerNumber,
            [FromQuery] string? trackingCode,
            [FromQuery] string? status,
            [FromQuery] DateOnly? pickUpDate,
            [FromQuery] DateOnly? deliveryDate
        )
        {
            var result = await _orderService.GetOrders(orderId, tripId, userId, containerType, containerNumber, trackingCode, status, pickUpDate, deliveryDate);
            return Ok(result);
        }

        [HttpGet("{orderId}/order-file")]
        public async Task<IActionResult> GetOrderFiles(string orderId)
        {
            var result = await _orderService.GetOrderFiles(orderId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrderWithFile([FromForm] OrderRequest orderRequest, [FromForm] List<string> descriptions, [FromForm] List<string> notes, [FromForm] List<IFormFile> files)
        {
            var currentUser = HttpContext.User;
            if (files.Count != descriptions.Count || files.Count != notes.Count)
            {
                return BadRequest("Số lượng files, descriptions và notes phải bằng nhau.");
            }

            var result = await _orderService.CreateOrder(orderRequest, currentUser, files, descriptions, notes);
            return Ok(result);
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportOrdersToExcel([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            try
            {
                var fileContent = await _orderService.ExportOrdersToExcelAsync(fromDate, toDate);
                var fileName = $"Danh sach don hang_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xuất file Excel: {ex.Message}");
            }
        }

        [HttpPut("update/{orderId}")]
        public async Task<IActionResult> UpdateOrderAsync(string orderId, [FromQuery] UpdateOrderRequest model)
        {
            var claims = User;

            var result = await _orderService.UpdateOrderAsync(orderId, model, claims);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
            {
                return Ok(result); 
            }

            return BadRequest(result); 
        }
    }
}