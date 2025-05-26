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
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }
        [Authorize]
        #region Get order by fillter
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? orderId = null,
            [FromQuery] string? tripId = null,
            [FromQuery] string? customerId = null,
            [FromQuery] string? trackingCode = null,
            [FromQuery] string? status = null
        )
        {
            var result = await _orderService.GetOrders(orderId, tripId, customerId, trackingCode, status);
            return Ok(result);
        }
        #endregion

        //[Authorize]
        //[HttpGet("{orderId}")]
        //public async Task<IActionResult> GetOrderDetailByOrderId(string orderId)
        //{
        //    var result = await _orderService.GetOrderDetailByOrderId(orderId);
        //    return Ok(result);
        //}

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateOrderWithFile([FromForm] OrderRequest orderRequest)
        {
            var currentUser = HttpContext.User;
            var result = await _orderService.CreateOrder(orderRequest, currentUser);
            return Ok(result);
        }

        [Authorize]
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

                var fileContent = await _orderService.ExportOrdersToExcelAsync(
                    fromDate.ToDateTime(TimeOnly.MinValue),
                    toDate.ToDateTime(TimeOnly.MinValue)
                );

                var fileName = $"Danh_sach_don_hang_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi xuất file Excel: {ex.Message}");
            }
        }

        [Authorize]
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
        //[HttpGet("{trackingCode}")]
        //public async Task<ActionResult<OrderDto>> GetOrderByTrackingCodeAsync(string trackingCode)
        //{
        //    var order = await _orderService.GetOrderByTrackingCodeAsync(trackingCode);
        //    if (order == null)
        //    {
        //        return NotFound(new { message = "Order not found" });
        //    }
        //    return Ok(order);
        //}
        #endregion


        [Authorize]
        [HttpPatch("{orderId}/toggle-is-pay")]
        public async Task<IActionResult> ToggleIsPay(string orderId)
        {
            try
            {
                var userClaims = User;

                var result = await _orderService.ToggleIsPayAsync(orderId, userClaims);

                if (result == null)
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

        //[HttpPost("cancel/{orderId}")]
        //public async Task<IActionResult> CancelOrder(string orderId)
        //{
        //    var result = await _orderService.CancelOrderAsync(orderId, User);

        //    if (result.Status == 1)
        //    {
        //        return Ok(new
        //        {
        //            message = result.Message
        //        });
        //    }

        //    return BadRequest(new
        //    {
        //        message = result.Message
        //    });
        //}
    }
}