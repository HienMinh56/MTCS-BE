using Microsoft.AspNetCore.Mvc;
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
            [FromQuery] string? userId,
            [FromQuery] int? containerType,
            [FromQuery] string? containerNumber,
            [FromQuery] string? trackingCode,
            [FromQuery] string? status,
            [FromQuery] DateOnly? pickUpDate,
            [FromQuery] DateOnly? deliveryDate
        )
        {
            var result = await _orderService.GetOrders(orderId, userId, containerType, containerNumber, trackingCode, status, pickUpDate, deliveryDate);
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
    }
}