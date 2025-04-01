using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data;
using MTCS.Data.Request;
using MTCS.Service.Base;
using MTCS.Service.Services;
using Sprache;
using System.Security.Claims;
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

        [HttpPut("update")]
        public async Task<IActionResult> UpdateOrder([FromForm] UpdateOrderRequest model)
        {
            var currentUser = HttpContext.User;
            var result = await _orderService.UpdateOrderAsync(model, currentUser);
            return Ok(result);
        }

        //[HttpGet("export")]
        //public async Task<IActionResult> ExportOrdersToExcel()
        //{
        //    // Lấy danh sách đơn hàng từ service
        //    var orders = await _orderService.GetOrders();

        //    // Gọi ExportOrdersToExcelAsync để xuất dữ liệu
        //    var result = await _orderService.ExportOrdersToExcelAsync(orders);

        //    if (result.Status == 1 && result.Data != null)
        //    {
        //        // Kiểm tra xem result.Data có phải là IEnumerable<Order>
        //        var filePath = result.Data.FilePath?.ToString();
        //        if (!string.IsNullOrEmpty(filePath))
        //        {
        //            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        //            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Orders.xlsx");
        //        }
        //        return BadRequest("No file path found.");
        //    }

        //    // Nếu có lỗi, trả về thông báo lỗi
        //    return BadRequest(result.Message);
        //}
        [HttpGet("export-orders")]
        public async Task<IActionResult> ExportOrdersToExcel()
        {
            var orders = await _orderService.GetAllOrders();  // Lấy danh sách đơn hàng từ service

            if (orders.Any())  // Kiểm tra nếu có đơn hàng
            {
                var result = await _orderService.ExportOrdersToExcelAsync(orders);  // Xuất ra file Excel

                if (result != null)  // Kiểm tra nếu xuất thành công
                {
                    return File(result, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "orders.xlsx");
                }

                return BadRequest("Export failed");
            }

            return NotFound("No orders found");
        }
    }
}
