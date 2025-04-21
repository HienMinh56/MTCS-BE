using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTCS.Common;
using MTCS.Data.DTOs;
using MTCS.Data.Request;
using MTCS.Service.Base;
using MTCS.Service.Services;
using static Google.Cloud.Firestore.V1.StructuredQuery.Types;

namespace MTCS.APIService.Controllers
{
    [Route("api/order")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        #region Get order by fillter
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
        #endregion

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
        public async Task<IActionResult> ExportOrdersToExcel([FromQuery] string fromDateStr, [FromQuery] string toDateStr)
        {
            try
            {
                if (!DateOnly.TryParseExact(fromDateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateOnly fromDate))
                {
                    return BadRequest("Định dạng ngày 'fromDate' không hợp lệ. Vui lòng sử dụng định dạng DD/MM/YYYY.");
                }

                if (!DateOnly.TryParseExact(toDateStr, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateOnly toDate))
                {
                    return BadRequest("Định dạng ngày 'toDate' không hợp lệ. Vui lòng sử dụng định dạng DD/MM/YYYY.");
                }
                var fileContent = await _orderService.ExportOrdersToExcelAsync(fromDate, toDate);
                var fileName = $"Danh_sach_don_hang_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xuất file Excel: {ex.Message}");
            }
        }

        [HttpPut("update/{orderId}")]
        public async Task<IActionResult> UpdateOrderAsync(string orderId, [FromForm] UpdateOrderRequest model)
        {
            var claims = User;

            var result = await _orderService.UpdateOrderAsync(orderId, model, claims);

            if (result.Status == Const.SUCCESS_UPDATE_CODE)
            {
                return Ok(result); 
            }

            return BadRequest(result); 
        }

        #region get order by tracking code
        /// <summary>
        /// Get order by tracking code - for customer search order
        /// </summary>
        /// <param name="trackingCode"></param>
        /// <returns></returns>
        [HttpGet("{trackingCode}")]
        public async Task<ActionResult<OrderDto>> GetOrderByTrackingCodeAsync(string trackingCode)
        {
            var order = await _orderService.GetOrderByTrackingCodeAsync(trackingCode);
            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }
            return Ok(order);
        }
        #endregion

        [HttpPatch("{orderId}/toggle-is-pay")]
        public async Task<IActionResult> ToggleIsPay(string orderId)
        {
            try
            {
                var userClaims = User;

                var result = await _orderService.ToggleIsPayAsync(orderId, userClaims);

                if (result==null)
                {
                    return Ok(result); 
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BusinessResult(Const.FAIL_UPDATE_CODE, ex.Message));
            }
        }
    }
}