using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.Request;
using MTCS.Service.Interfaces;
using MTCS.Service.Services;

namespace MTCS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderDetailController : ControllerBase
    {
        private readonly IOrderDetailService _orderDetailService;

        public OrderDetailController(IOrderDetailService orderDetailService)
        {
            _orderDetailService = orderDetailService;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrderDetailAsync([FromForm] OrderDetailRequest orderRequest, [FromForm] List<string> descriptions, [FromForm] List<string> notes, [FromForm] List<IFormFile> files)
        {
            var currentUser = HttpContext.User;
            if (files.Count != descriptions.Count || files.Count != notes.Count)
            {
                return BadRequest("Số lượng files, descriptions và notes phải bằng nhau.");
            }

            var result = await _orderDetailService.CreateOrderDetailAsync(orderRequest, currentUser, files, descriptions, notes);
            return Ok(result);
        }

        #region Get order detail by filter
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(
        [FromQuery] string? orderId,
        [FromQuery] string? containerNumber,
        [FromQuery] DateOnly? pickUpDate,
        [FromQuery] DateOnly? deliveryDate,
        [FromQuery] string? driverId)
        {
            var result = await _orderDetailService.GetOrderDetailsAsync(orderId, containerNumber, pickUpDate, deliveryDate, driverId);
            return Ok(result);
        }
        #endregion

        [HttpPut("{orderDetailId}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderDetail(string orderDetailId, [FromForm] UpdateOrderDetailRequest model)
        {
            var claims = HttpContext.User;

            var result = await _orderDetailService.UpdateOrderDetailAsync(orderDetailId, model, claims);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
            {
                return Ok(result);
            }

            return BadRequest(result);
        }
    }
}
