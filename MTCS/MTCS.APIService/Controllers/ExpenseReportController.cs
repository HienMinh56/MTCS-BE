using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MTCS.Data.Request;
using MTCS.Service.Services;

namespace MTCS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExpenseReportController : ControllerBase
    {
        private readonly IExpenseReportService _expenseReportService;
        public ExpenseReportController(IExpenseReportService expenseReportService)
        {
            _expenseReportService = expenseReportService;
        }


        [HttpGet("GetAllExpenseReports")]
        public async Task<IActionResult> GetAllExpenseReports(string? driverId, string? orderid, string? tripId, string? reportId, int? isPay)
        {
            var result = await _expenseReportService.GetAllExpenseReports(driverId, orderid, tripId, reportId, isPay);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }

        [HttpGet("GetExpenseReportById/{id}")]
        public async Task<IActionResult> GetExpenseReportById(string id)
        {
            var result = await _expenseReportService.GetExpenseReportById(id);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }

        [HttpPost("CreateExpenseReport")]
        public async Task<IActionResult> CreateExpenseReport([FromForm] CreateExpenseReportRequest expenseReport)
        {
            var user = HttpContext.User;
            var result = await _expenseReportService.CreateExpenseReport(expenseReport, expenseReport.Files, user);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }

        [HttpPut("UpdateExpenseReport")]
        public async Task<IActionResult> UpdateExpenseReport([FromBody] UpdateExpenseReportRequest expenseReport)
        {
            var user = HttpContext.User;
            var result = await _expenseReportService.UpdateExpenseReport(expenseReport, user);
            if (result.Status == 200)
            {
                return Ok(result);
            }
            return NotFound(result);
        }

        //[HttpDelete("DeleteExpenseReport/{id}")]
        //public async Task<IActionResult> DeleteExpenseReport(string id)
        //{
        //    var result = await _expenseReportService.DeleteExpenseReport(id);
        //    if (result.Status == 200)
        //    {
        //        return Ok(result);
        //    }
        //    return NotFound(result);
        //}
    }
}
